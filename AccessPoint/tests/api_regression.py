"""HTTP regression checks against a fresh seeded development database."""
import json, os, time, uuid, urllib.request, urllib.error, http.cookiejar
from datetime import datetime, timedelta, timezone
BASE=os.environ.get("APP_BASE","http://127.0.0.1:5080")
PASSWORD=os.environ["TEST_PASSWORD"]
checks=0
def expect(condition,label):
    global checks
    if not condition: raise AssertionError(label)
    checks+=1
    print("PASS",label)
class Client:
    def __init__(self):
        self.http=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        self.csrf=None
    def call(self,path,method="GET",data=None,headers=None,csrf=True):
        h={"Content-Type":"application/json"}
        if self.csrf and csrf:h["X-CSRF-TOKEN"]=self.csrf
        h.update(headers or {})
        req=urllib.request.Request(BASE+path,method=method,headers=h,data=None if data is None else json.dumps(data).encode())
        try:response=self.http.open(req,timeout=10)
        except urllib.error.HTTPError as error:response=error
        raw=response.read().decode()
        try:body=json.loads(raw) if raw else None
        except json.JSONDecodeError:body=raw
        return response.status,body
    def session(self):
        code,body=self.call("/api/account/session")
        expect(code==200,"session endpoint")
        self.csrf=body["csrfToken"]
        return body
    def login(self,email):
        self.session()
        code,body=self.call("/api/account/login","POST",{"email":email,"password":PASSWORD})
        expect(code==200,"login "+email)
        self.session()
for attempt in range(120):
    try:
        with urllib.request.urlopen(BASE+"/health/ready",timeout=1) as response:
            if response.status==200:break
    except (OSError,urllib.error.URLError):time.sleep(.25)
else:raise RuntimeError("Server failed to start; inspect its log")
anon=Client();anon.session()
expect(anon.call("/api/account/login","POST",{"email":"citizen@example.test","password":PASSWORD},csrf=False)[0]==400,"login requires antiforgery token")
citizen=Client();citizen.login("citizen@example.test")
other=Client();other.login("other@example.test")
staff=Client();staff.login("staff@example.test")
admin=Client();admin.login("admin@example.test")
expect(anon.call("/api/bookings")[0]==401,"bookings require authentication")
_,catalog=citizen.call("/api/catalog")
provider=catalog["providers"][0]["id"]
date=datetime.now(timezone.utc).date()+timedelta(days=1)
while date.weekday()>=5:date+=timedelta(days=1)
_,slots=citizen.call("/api/slots?providerId="+provider+"&date="+date.isoformat())
expect(len(slots)>=2,"seeded schedule availability")
payload={"slotId":slots[0]["id"],"service":"Application assistance","format":"Video"}
key=str(uuid.uuid4());headers={"Idempotency-Key":key}
expect(citizen.call("/api/bookings","POST",payload)[0]==400,"idempotency key required")
code,booking=citizen.call("/api/bookings","POST",payload,headers)
expect(code==201,"reservation persisted")
code,replay=citizen.call("/api/bookings","POST",payload,headers)
expect(code==200 and replay["id"]==booking["id"],"idempotent replay returns same booking")
expect(citizen.call("/api/bookings","POST",{**payload,"format":"Phone"},headers)[0]==409,"key reuse with different payload rejected")
expect(other.call("/api/bookings/"+booking["id"])[0]==404,"other citizen cannot read booking")
expect(other.call("/api/bookings","POST",payload,{"Idempotency-Key":str(uuid.uuid4())})[0]==409,"double booking rejected")
expect(citizen.call("/api/bookings/"+booking["id"]+"/actions","POST",{"version":booking["version"],"action":"CheckedIn"})[0]==409,"citizen cannot check in")
code,rescheduled=citizen.call("/api/bookings/"+booking["id"]+"/reschedule","POST",{"version":booking["version"],"slotId":slots[1]["id"]})
expect(code==200 and rescheduled["slotId"]==slots[1]["id"],"reschedule saved")
expect(citizen.call("/api/bookings/"+booking["id"]+"/actions","POST",{"version":booking["version"],"action":"Cancelled"})[0]==409,"stale version rejected")
code,cancelled=citizen.call("/api/bookings/"+booking["id"]+"/actions","POST",{"version":rescheduled["version"],"action":"Cancelled"})
expect(code==200 and cancelled["status"]=="Cancelled","cancellation saved")
_,available=citizen.call("/api/slots?providerId="+provider+"&date="+date.isoformat())
expect(slots[1]["id"] in [s["id"] for s in available],"cancelled slot released")
code,booking2=citizen.call("/api/bookings","POST",payload,{"Idempotency-Key":str(uuid.uuid4())})
expect(code==201,"released original slot reservable")
code,checked=staff.call("/api/bookings/"+booking2["id"]+"/actions","POST",{"version":booking2["version"],"action":"CheckedIn"})
expect(code==200 and checked["status"]=="CheckedIn","staff check-in")
code,complete=staff.call("/api/bookings/"+booking2["id"]+"/actions","POST",{"version":checked["version"],"action":"Completed"})
expect(code==200 and complete["status"]=="Completed","staff completion")
_,history=citizen.call("/api/bookings/"+booking2["id"]+"/history")
expect(len(history)==3,"booking audit entries persisted")
expect(citizen.call("/api/slots","POST",{"providerId":provider,"startUtc":date.isoformat()+"T17:00:00Z"})[0]==403,"schedule publication admin-only")
expect(admin.call("/api/slots","POST",{"providerId":provider,"startUtc":date.isoformat()+"T17:00:00Z"})[0]==200,"admin publishes schedule")
for attempt in range(100):
    code,operations=admin.call("/api/operations")
    if code==200 and operations["localDeliveries"]>0:break
    time.sleep(.25)
expect(operations["localDeliveries"]>0,"outbox processed into durable local sink")
expect(citizen.call("/api/operations")[0]==403,"operations monitor admin-only")
expect(citizen.call("/api/account/logout","POST")[0]==204,"logout")
print(f"{checks} HTTP assertions passed.")
