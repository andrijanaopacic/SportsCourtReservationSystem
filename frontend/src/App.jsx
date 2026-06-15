import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';

import Navbar from './components/Navbar';

import SportsList from './pages/SportsList';
import SportForm from './pages/SportForm';

import CourtsList from './pages/CourtsList';
import CourtForm from './pages/CourtForm';

import CourtReserve from './pages/CourtReserve';

import TimeSlotsList from './pages/TimeSlotsList';
import TimeSlotForm from './pages/TimeSlotForm';

import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';

import CourtCalendar from './pages/CourtCalendar';
import CourtBooking from './pages/CourtBooking';

import ConfirmReservation from './pages/ConfirmReservation';
import Reservation from './pages/Reservation';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Navbar />

        <Routes>
          {/* SPORTS */}
          <Route path="/" element={<SportsList />} />
          <Route path="/sports/new" element={<SportForm />} />
          <Route path="/sports/:id" element={<SportForm />} />

          {/* COURTS */}
          <Route path="/courts" element={<CourtsList />} />
          <Route path="/courts/new" element={<CourtForm />} />
          <Route path="/courts/:id" element={<CourtForm />} />

          {/* TIMESLOTS */}
          <Route path="/timeslots" element={<TimeSlotsList />} />
          <Route path="/timeslots/new" element={<TimeSlotForm />} />
          <Route path="/timeslots/:id" element={<TimeSlotForm />} />

          <Route path="/reservations/create" element={<CourtReserve />} />
          <Route path="/courts/:courtId/reserve" element={<CourtBooking />} />
          <Route path="/courts/:courtId/calendar" element={<CourtCalendar />} />
         
          <Route path="/confirm-reservation" element={<ConfirmReservation />} />
          <Route path="/reservations" element={<Reservation />} />

          {/* AUTH */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;