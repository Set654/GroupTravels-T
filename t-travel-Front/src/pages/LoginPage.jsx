import { useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { AuthCard, PageHeading, Tabs, FormField, ActionButton, CenterActions } from '../components/AuthUI.jsx';
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
    } catch (error) {
      window.alert(error.message);
    } finally {
      setBusy(false);
    }
  }

  return <AuthCard variant={isSMS ? 'login-sms' : 'login'}>
    <PageHeading title="С возвращением!">Войдите, чтобы запланировать поездку.</PageHeading>
    <Tabs className="auth-main-tabs" value="login" onChange={v => v === 'register' && navigate('/register')}
      options={[{ value: 'login', label: 'Вход' }, { value: 'register', label: 'Регистрация' }]} />

    <section className="login-method">
      <h2 className="section-label">Способ входа</h2>
      <Tabs value={method} onChange={v => setParams(v === 'sms' ? { method: 'sms' } : {})}
        options={[{ value: 'sms', label: 'SMS-код' }, { value: 'email', label: 'Email или телефон' }]} />
    </section>

    <form className="login-form" onSubmit={submit} noValidate>
      {isSMS ? <FormField label="Номер телефона" type="tel" autoComplete="tel" value={phone}
        placeholder="Например, +7 800 555-35-35" onChange={e => setPhone(e.target.value)} /> : <>
        <FormField label="Email или номер телефона" autoComplete="username" value={contact}
          placeholder="Например, makarena@mail.ru или +7 800 555-35-35"
          onChange={e => setContact(e.target.value)} />
        <FormField className="login-password" label="Пароль" autoComplete="current-password"
          type={passwordVisible ? 'text' : 'password'} value={password} placeholder="Не менее 8 символов"
          onChange={e => setPassword(e.target.value)} toggleVisible={passwordVisible}
          onToggleVisible={() => setPasswordVisible(v => !v)} rightLink="Забыли пароль?"
          rightLinkClick={() => navigate('/forgot-password')} />
      </>}
      <CenterActions className="login-submit">
        <ActionButton type="submit" size="small" disabled={busy}>{isSMS ? 'Получить код' : 'Войти'}</ActionButton>
      </CenterActions>
    </form>
  </AuthCard>;
}
