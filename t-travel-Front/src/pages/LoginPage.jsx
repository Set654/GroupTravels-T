import { useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { SvgStage, Hotspot, SvgInput } from '../components/SvgStage.jsx';
import { authApi } from '../lib/api.js';
import { extractToken, saveToken } from '../lib/authStorage.js';
import { isValidEmail, isValidPhone, normalizePhone } from '../lib/validators.js';

export default function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [params, setParams] = useSearchParams();
  const method = params.get('method') === 'sms' ? 'sms' : 'email';
  const [contact, setContact] = useState(location.state?.email || '');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [busy, setBusy] = useState(false);
  const isSMS = method === 'sms';

  async function submit(event) {
    event.preventDefault();
    if (busy) return;
    if (isSMS && !isValidPhone(phone)) return window.alert('Введите номер телефона.');
    if (!isSMS && !(isValidEmail(contact) || isValidPhone(contact))) return window.alert('Введите Email или номер телефона.');
    if (!isSMS && !password) return window.alert('Введите пароль.');
    setBusy(true);
    try {
      if (isSMS) {
        const number = normalizePhone(phone);
        await authApi.sendPhoneCode(number);
        navigate('/verify-phone', { state: { phone: number } });
      } else {
        const data = await authApi.login(contact.trim(), password);
        const token = extractToken(data);
        if (!token) throw new Error('Сервер не вернул JWT-токен.');
        saveToken(token);
        navigate('/welcome', { replace: true });
      }
    } catch (error) { window.alert(error.message); }
    finally { setBusy(false); }
  }

  return <SvgStage name={isSMS ? 'Log_in_SMS.svg' : 'Log_in_Email-number.svg'} height={isSMS ? 452 : 542}>
    <Hotspot x={32} y={112} w={233} h={40} label="Вход" onClick={() => navigate('/login')} />
    <Hotspot x={285} y={112} w={233} h={40} label="Регистрация" onClick={() => navigate('/register')} />
    <Hotspot x={32} y={206} w={233} h={48} label="SMS-код" onClick={() => setParams({ method: 'sms' })} />
    <Hotspot x={285} y={206} w={233} h={48} label="Email или телефон" onClick={() => setParams({})} />
    <form className="svg-stage-form" onSubmit={submit} noValidate>
      {isSMS ? <SvgInput x={32} y={308} w={486} label="Номер телефона" type="tel"
        autoComplete="tel" value={phone} onChange={(event) => setPhone(event.target.value)} /> : <>
        <SvgInput x={32} y={308} w={486} label="Email или номер телефона"
          autoComplete="username" value={contact} onChange={(event) => setContact(event.target.value)} />
        <SvgInput x={32} y={398} w={446} label="Пароль"
          autoComplete="current-password" type={passwordVisible ? 'text' : 'password'} value={password}
          onChange={(event) => setPassword(event.target.value)} />
        <Hotspot x={473} y={399} w={37} h={45} label={passwordVisible ? 'Скрыть пароль' : 'Показать пароль'} onClick={() => setPasswordVisible(!passwordVisible)} />
        <Hotspot x={392} y={363} w={129} h={33} label="Забыли пароль?" onClick={() => navigate('/forgot-password')} />
      </>}
      <Hotspot x={198} y={isSMS ? 388 : 478} w={154} h={40} type="submit" disabled={busy}
        label={isSMS ? 'Получить код' : 'Войти'} />
    </form>
  </SvgStage>;
}
