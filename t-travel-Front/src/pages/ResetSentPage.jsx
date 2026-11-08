import { useLocation, useNavigate } from 'react-router-dom';
import { SvgStage, Hotspot, OverlayText } from '../components/SvgStage.jsx';

export default function ResetSentPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const contact = state?.contact || 'makarena@mail.ru';
  return <SvgStage name="Reset_password.svg" height={188}>
    {contact !== 'makarena@mail.ru' && <OverlayText x={32} y={72} w={486} h={22}>
      На {contact} будет отправлен код для сброса пароля.
    </OverlayText>}
    <Hotspot x={178} y={124} w={194} h={40} label="Ввести код"
      onClick={() => navigate('/reset-code', { state: { contact, type: state?.type || 'email' } })} />
  </SvgStage>;
}
