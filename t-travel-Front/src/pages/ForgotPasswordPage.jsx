import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthCard, PageHeading, Tabs, FormField, ActionButton, CenterActions } from '../components/AuthUI.jsx';
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
    } catch (error) {
      window.alert(error.message);
    } finally {
      setBusy(false);
    }
  }

  return <AuthCard variant="forgot">
    <PageHeading title="Сброс пароля">Укажите Email или номер телефона, связанный с аккаунтом.<br/>Мы отправим вам код для сброса пароля.</PageHeading>
    <Tabs value={method} onChange={setMethod} options={[{ value: 'email', label: 'Email' }, { value: 'phone', label: 'Телефон' }]} />
    <form className="forgot-form" onSubmit={submit} noValidate>
      {method === 'email'
        ? <FormField label="Email" type="email" autoComplete="email" value={email} placeholder="Например, makarena@mail.ru" onChange={e => setEmail(e.target.value)} />
        : <FormField label="Номер телефона" type="tel" autoComplete="tel" value={phone} placeholder="Например, +7 800 555-35-35" onChange={e => setPhone(e.target.value)} />}
      <CenterActions className="forgot-submit">
        <ActionButton type="submit" size="small" disabled={busy}>Получить код</ActionButton>
      </CenterActions>
    </form>
  </AuthCard>;
}
