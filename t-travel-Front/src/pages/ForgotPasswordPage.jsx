import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { SvgStage, Hotspot, SvgInput } from '../components/SvgStage.jsx';
import { authApi } from '../lib/api.js';
import { isValidEmail, isValidPhone, normalizePhone } from '../lib/validators.js';

export default function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [method, setMethod] = useState('email');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(event) {
    event.preventDefault();
    if (busy) return;
    const value = method === 'email' ? email.trim() : normalizePhone(phone);
    if (method === 'email' && !isValidEmail(value)) return window.alert('Введите корректный Email.');
    if (method === 'phone' && !isValidPhone(phone)) return window.alert('Введите корректный номер телефона.');
    setBusy(true);
    try {
      await authApi.requestPasswordReset(value, method);
      navigate('/reset-sent', { state: { contact: value, type: method } });
    } catch (e) { window.alert(e.message); }
    finally { setBusy(false); }
  }
  return <SvgStage name={method === 'email' ? 'Reset_password-Email.svg' : 'Reset_password-number.svg'} height={372}>
    <Hotspot x={32} y={134} w={233} h={40} label="Email" onClick={() => setMethod('email')} />
    <Hotspot x={285} y={134} w={233} h={40} label="Телефон" onClick={() => setMethod('phone')} />
    <form className="svg-stage-form" onSubmit={submit} noValidate>
      {method === 'email' ?
        <SvgInput x={32} y={228} w={486} type="email" autoComplete="email" label="Email" value={email} onChange={(e) => setEmail(e.target.value)} /> :
        <SvgInput x={32} y={228} w={486} type="tel" autoComplete="tel" label="Номер телефона" value={phone} onChange={(e) => setPhone(e.target.value)} />}
      <Hotspot x={198} y={308} w={154} h={40} type="submit" disabled={busy} label="Получить код" />
    </form>
  </SvgStage>;
}
