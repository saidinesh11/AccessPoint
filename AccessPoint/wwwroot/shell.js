import {session, request, announce} from './api.js';
export async function initialize() {
  document.getElementById('login-form').onsubmit=async event=>{
    event.preventDefault();const button=event.submitter;button.disabled=true;
    try {await request('/api/account/login','POST',{email:document.getElementById('email').value,password:document.getElementById('password').value});location.reload();}
    catch(error){announce(error.message,true);}finally{button.disabled=false;}
  };
  document.getElementById('logout').onclick=async()=>{await request('/api/account/logout','POST');location.reload();};
  const current=await session();
  if(!current.authenticated)return null;
  document.getElementById('login-panel').hidden=true;document.getElementById('workspace').hidden=false;document.getElementById('logout').hidden=false;
  document.getElementById('identity').textContent=current.user.displayName+' · '+current.user.roles.join(', ');
  if(current.user.roles.includes('Admin')){
    document.getElementById('operations').hidden=false;
    document.getElementById('refresh-operations').onclick=async()=>{try{document.getElementById('operations-data').textContent=JSON.stringify(await request('/api/operations'),null,2);}catch(e){announce(e.message,true)}};
  }
  return current.user;
}
