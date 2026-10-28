import {initialize} from './shell.js';
import {request,text,announce,formatDate} from './api.js';
const $=id=>document.getElementById(id);
let user,staff=false,page=1,slot=null,rescheduling=null,commandKey=null,pendingAction=null;
function option(select,value,label){const item=text('option',label);item.value=value;select.append(item);}
async function slots(){
  slot=null;commandKey=null;$('selection').textContent='No slot selected.';$('slots').replaceChildren();
  const values=await request('/api/slots?'+new URLSearchParams({providerId:$('provider').value,date:$('date').value}));
  if(!values.length)$('slots').append(text('p','No slots available for this date. Try another date.','muted'));
  for(const item of values){const b=text('button',formatDate(item.startUtc)+' · '+item.provider,'slot secondary');b.type='button';b.setAttribute('aria-pressed','false');b.onclick=()=>{
    slot=item;commandKey=crypto.randomUUID();for(const sibling of $('slots').children)sibling.setAttribute('aria-pressed','false');b.setAttribute('aria-pressed','true');$('selection').textContent='Selected '+formatDate(item.startUtc);
  };$('slots').append(b)}
}
function makeAction(label,action){const b=text('button',label,'secondary');b.type='button';b.onclick=action;return b;}
async function refresh(){
  const result=await request('/api/bookings?'+new URLSearchParams({status:$('status-filter').value,page}));$('bookings').replaceChildren();
  $('count').textContent=result.total+' appointments';$('page-label').textContent='Page '+page;$('previous').disabled=page<=1;$('next').disabled=page*10>=result.total;
  for(const b of result.items){const tr=document.createElement('tr'),info=document.createElement('td'),state=text('td',b.status),actions=document.createElement('td');
    info.append(text('strong',b.service),text('p',formatDate(b.slot.startUtc)+' · '+b.slot.provider.name,'muted'));
    actions.append(makeAction('History',async()=>{try{const history=await request('/api/bookings/'+b.id+'/history');$('history').replaceChildren(...history.map(e=>text('li',e.action+' · '+e.detail+' · '+formatDate(e.createdUtc))));$('history-dialog').showModal();}catch(e){announce(e.message,true)}}));
    if(b.status==='Reserved')actions.append(makeAction('Reschedule',()=>{rescheduling=b;$('booking-heading').textContent='Reschedule appointment';$('stop-reschedule').hidden=false;$('reserve').textContent='Confirm reschedule';$('service').value=b.service;$('format').value=b.format;$('provider').value=b.slot.providerId;$('date').focus();announce('Choose a new date and slot, then confirm reschedule.');}));
    const choices=[];if(['Reserved','CheckedIn'].includes(b.status))choices.push(['Cancel','Cancelled']);if(staff&&b.status==='Reserved')choices.push(['Check in','CheckedIn']);if(staff&&b.status==='CheckedIn')choices.push(['Complete','Completed']);
    for(const [label,action]of choices)actions.append(makeAction(label,()=>{pendingAction={booking:b,action};$('action-title').textContent=label+' appointment?';$('action-description').textContent=b.service+' · '+formatDate(b.slot.startUtc);$('action-dialog').showModal();}));
    tr.append(info,state,actions);$('bookings').append(tr);
  }
}
function resetMode(){rescheduling=null;commandKey=null;slot=null;$('booking-heading').textContent='Reserve a time';$('reserve').textContent='Confirm reservation';$('stop-reschedule').hidden=true;}
$('booking-form').onsubmit=async event=>{event.preventDefault();if(!slot){announce('Select an available slot first.',true);return}const button=$('reserve');button.disabled=true;
  try{if(rescheduling)await request('/api/bookings/'+rescheduling.id+'/reschedule','POST',{slotId:slot.id,version:rescheduling.version});
    else await request('/api/bookings','POST',{slotId:slot.id,service:$('service').value,format:$('format').value},{'Idempotency-Key':commandKey});
    resetMode();await Promise.all([refresh(),slots()]);announce('Appointment saved. The latest booking is shown in the queue.');
  }catch(e){announce(e.message,true)}finally{button.disabled=false}
};
$('confirm-action').onclick=async()=>{if(!pendingAction)return;const button=$('confirm-action');button.disabled=true;try{await request('/api/bookings/'+pendingAction.booking.id+'/actions','POST',{version:pendingAction.booking.version,action:pendingAction.action});$('action-dialog').close();await refresh();announce('Appointment updated.');}catch(e){$('action-dialog').close();announce(e.message,true)}finally{button.disabled=false}};
$('close-action').onclick=()=>$('action-dialog').close();$('close-history').onclick=()=>$('history-dialog').close();
$('stop-reschedule').onclick=()=>{resetMode();announce('Rescheduling cancelled. Your original appointment is unchanged.');};
$('find-slots').onclick=()=>slots().catch(e=>announce(e.message,true));$('status-filter').onchange=()=>{page=1;refresh().catch(e=>announce(e.message,true))};
$('previous').onclick=()=>{page--;refresh().catch(e=>announce(e.message,true))};$('next').onclick=()=>{page++;refresh().catch(e=>announce(e.message,true))};
$('slot-form').onsubmit=async event=>{event.preventDefault();const button=event.submitter;button.disabled=true;try{await request('/api/slots','POST',{providerId:$('new-provider').value,startUtc:$('new-start').value+':00Z'});announce('Schedule slot published.');}catch(e){announce(e.message,true)}finally{button.disabled=false}};
async function start(){user=await initialize();if(!user)return;staff=user.roles.some(r=>['Staff','Admin'].includes(r));$('queue-heading').textContent=staff?'Staff appointment queue':'My appointments';$('admin-slots').hidden=!user.roles.includes('Admin');
  const data=await request('/api/catalog');for(const s of data.services)option($('service'),s,s);for(const p of data.providers){option($('provider'),p.id,p.name+' · '+p.specialty);option($('new-provider'),p.id,p.name)}
  const date=new Date();date.setUTCDate(date.getUTCDate()+1);while([0,6].includes(date.getUTCDay()))date.setUTCDate(date.getUTCDate()+1);$('date').value=date.toISOString().slice(0,10);$('date').min=new Date().toISOString().slice(0,10);await Promise.all([refresh(),slots()]);}
start().catch(e=>announce(e.message,true));
