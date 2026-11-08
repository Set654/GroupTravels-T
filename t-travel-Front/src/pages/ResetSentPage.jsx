import { useLocation, useNavigate } from 'react-router-dom';
import { AuthCard, PageHeading, ActionButton, CenterActions } from '../components/AuthUI.jsx';

export default function ResetSentPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const contact = state?.contact || 'makarena@mail.ru';
  return <AuthCard variant="reset-sent">
    <PageHeading title="Сброс пароля">На {contact} будет отправлен код для сброса пароля.</PageHeading>
    <CenterActions className="reset-sent-actions">
      <ActionButton onClick={() => navigate('/reset-code', { state: { contact, type: state?.type || 'email' } })}>Ввести код</ActionButton>
    </CenterActions>
  </AuthCard>;
}
