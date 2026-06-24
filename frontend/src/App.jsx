import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';

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

import CourtBooking from './pages/CourtBooking';

import ConfirmReservation from './pages/ConfirmReservation';
import Reservation from './pages/Reservation';
import AdminReservations from './pages/AdminReservations';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Navbar />

        <Routes>
          {/* PUBLIC */}
          
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* KLIJENT ONLY */}
          <Route path="/reservations/create" element={
            <ProtectedRoute role="client"><CourtReserve /></ProtectedRoute>
          } />
          <Route path="/courts/:courtId/reserve" element={
            <ProtectedRoute role="client"><CourtBooking /></ProtectedRoute>
          } />
          <Route path="/confirm-reservation" element={
            <ProtectedRoute role="client"><ConfirmReservation /></ProtectedRoute>
          } />
          <Route path="/reservations" element={
            <ProtectedRoute role="client"><Reservation /></ProtectedRoute>
          } />

          {/* ADMIN ONLY */}
          <Route path="/admin/reservations" element={
            <ProtectedRoute role="admin"><AdminReservations /></ProtectedRoute>
          } />
          <Route path="/" element={
            <ProtectedRoute role="admin"><SportsList /></ProtectedRoute>
          } />
          <Route path="/sports/new" element={
            <ProtectedRoute role="admin"><SportForm /></ProtectedRoute>
          } />
          <Route path="/sports/:id" element={
            <ProtectedRoute role="admin"><SportForm /></ProtectedRoute>
          } />

          <Route path="/courts" element={
            <ProtectedRoute role="admin"><CourtsList /></ProtectedRoute>
          } />
          <Route path="/courts/new" element={
            <ProtectedRoute role="admin"><CourtForm /></ProtectedRoute>
          } />
          <Route path="/courts/:id" element={
            <ProtectedRoute role="admin"><CourtForm /></ProtectedRoute>
          } />

          <Route path="/timeslots" element={
            <ProtectedRoute role="admin"><TimeSlotsList /></ProtectedRoute>
          } />
          <Route path="/timeslots/new" element={
            <ProtectedRoute role="admin"><TimeSlotForm /></ProtectedRoute>
          } />
          <Route path="/timeslots/:id" element={
            <ProtectedRoute role="admin"><TimeSlotForm /></ProtectedRoute>
          } />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
