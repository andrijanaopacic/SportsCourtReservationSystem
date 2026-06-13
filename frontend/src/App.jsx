import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import SportsList from './pages/SportsList';
import SportForm from './pages/SportForm';
import CourtsList from './pages/CourtsList';
import CourtForm from './pages/CourtForm';

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route path="/" element={<SportsList />} />
        <Route path="/sports/new" element={<SportForm />} />
        <Route path="/sports/:id" element={<SportForm />} />
        <Route path="/courts" element={<CourtsList />} />
        <Route path="/courts/new" element={<CourtForm />} />
        <Route path="/courts/:id" element={<CourtForm />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;