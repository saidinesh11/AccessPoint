let csrfToken = '';
export async function session() {
  const response = await fetch('/api/account/session', {credentials: 'same-origin'});
  if (!response.ok) throw new Error('Unable to establish a session.');
  const value = await response.json(); csrfToken = value.csrfToken; return value;
}
export async function request(path, method='GET', body, extraHeaders={}) {
  const response = await fetch(path, {method, credentials:'same-origin',
    headers:{'Content-Type':'application/json','X-CSRF-TOKEN':csrfToken,...extraHeaders},
    body:body===undefined?undefined:JSON.stringify(body)});
  if (!response.ok) {
    const problem=await response.json().catch(()=>({}));
    const error=new Error(problem.detail||Object.values(problem.errors||{}).flat().join(' ')||'Request failed.');
    error.status=response.status; throw error;
  }
  return response.status===204?null:response.json();
}
export function text(tag, content, className='') {const node=document.createElement(tag);node.textContent=content;if(className)node.className=className;return node;}
export function announce(message, error=false) {const el=document.getElementById('notice');el.textContent=message;el.className=error?'notice error':'notice';el.hidden=false;el.focus();}
export function formatDate(value) {return new Date(value).toLocaleString(undefined,{timeZone:'UTC',dateStyle:'medium',timeStyle:'short'})+' UTC';}
