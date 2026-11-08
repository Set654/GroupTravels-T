import { useNavigate } from 'react-router-dom';
import { AuthCard, PageHeading, ActionButton, CenterActions } from '../components/AuthUI.jsx';

export default function WelcomePage() {
  const navigate = useNavigate();
  const tripsBase = import.meta.env.VITE_TRIPS_URL;
  function openTrips(path) {
    if (tripsBase) {
      window.location.assign(`${tripsBase.replace(/\/$/, '')}${path}`);
      return;
    }
    navigate(path);
  }

  return <AuthCard variant="welcome">
    <PageHeading title="Всё готово к путешествиям!">Создайте первую поездку, пригласите друзей и начните<br className="desktop-only"/> записывать общие расходы.</PageHeading>
    <CenterActions className="welcome-actions">
      <ActionButton onClick={() => openTrips('/new')}>Создать поездку</ActionButton>
      <ActionButton variant="secondary" onClick={() => openTrips('/dashboard')}>Мои поездки</ActionButton>
    </CenterActions>
  </AuthCard>;
}
