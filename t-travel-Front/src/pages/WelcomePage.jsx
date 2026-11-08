import { SvgStage, Hotspot } from '../components/SvgStage.jsx';
import { useNavigate } from 'react-router-dom';

export default function WelcomePage() {
  const navigate = useNavigate();
  function openTrips(path) {
    const base = import.meta.env.VITE_TRIPS_URL;
    if (base) { window.location.assign(`${base.replace(/\/$/, '')}${path}`); return; }
    window.alert('Экран поездок пока не подключён.');
  }
  return <SvgStage name="Welcome.svg" height={254}>
    <Hotspot x={178} y={146} w={194} h={40} label="Создать поездку" onClick={() => openTrips('/new')} />
    <Hotspot x={178} y={190} w={194} h={40} label="Мои поездки" onClick={() => navigate('/dashboard')} />
  </SvgStage>;
}
