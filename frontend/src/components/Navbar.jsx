import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

function Navbar() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logoutUser, isAdmin } = useAuth();

  const isActive = (path) => location.pathname === path || location.pathname.startsWith(path + '/');

  const handleLogout = () => {
    logoutUser();
    navigate('/login');
  };

  const links = [
    { path: '/', label: 'Sports' },
    { path: '/courts', label: 'Courts' },
    { path: '/timeslots', label: 'Time Slots' },
  ];

  return (
    <>
      <nav style={{
        background: 'var(--mahogany)', padding: '0 48px',
        display: 'flex', alignItems: 'center',
        justifyContent: 'space-between', height: '64px'
      }}>
        <Link to="/" style={{
          fontFamily: 'Cormorant Garamond, serif', fontSize: '22px',
          fontWeight: 300, letterSpacing: '0.2em', color: 'var(--cream)',
          textTransform: 'uppercase', textDecoration: 'none'
        }}>
          Court Reserve
        </Link>

        <div style={{ display: 'flex', alignItems: 'center', gap: '36px' }}>
          {links.map(({ path, label }) => (
            <Link key={path} to={path} style={{
              fontFamily: 'Inter, sans-serif', fontSize: '11px',
              letterSpacing: '0.15em', textTransform: 'uppercase',
              color: isActive(path) ? 'var(--cream)' : 'var(--sage)',
              textDecoration: 'none', transition: 'color 0.2s'
            }}>
              {label}
            </Link>
          ))}

          {/* Separator */}
          <div style={{ width: '1px', height: '20px', background: 'rgba(176,186,153,0.3)' }} />

          {user ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
                <span style={{ fontSize: '12px', color: 'var(--cream)', letterSpacing: '0.05em' }}>
                  {user.firstName} {user.lastName}
                </span>
                <span style={{ fontSize: '9px', color: 'var(--sage)', letterSpacing: '0.12em', textTransform: 'uppercase' }}>
                  {isAdmin() ? 'Admin' : 'Member'}
                </span>
              </div>
              <button
                onClick={handleLogout}
                style={{
                  background: 'transparent', border: '1px solid var(--sage)',
                  color: 'var(--sage)', padding: '5px 14px',
                  fontSize: '10px', letterSpacing: '0.12em',
                  textTransform: 'uppercase', cursor: 'pointer',
                  transition: 'all 0.2s',
                }}
              >
                Sign Out
              </button>
            </div>
          ) : (
            <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
              <Link to="/login" style={{
                fontSize: '11px', letterSpacing: '0.12em',
                textTransform: 'uppercase', color: 'var(--sage)',
                textDecoration: 'none', transition: 'color 0.2s',
              }}>
                Sign In
              </Link>
              <Link to="/register" style={{
                fontSize: '11px', letterSpacing: '0.12em',
                textTransform: 'uppercase', color: 'var(--cream)',
                textDecoration: 'none', border: '1px solid var(--cream)',
                padding: '5px 14px',
              }}>
                Register
              </Link>
            </div>
          )}
        </div>
      </nav>
      <div style={{ height: '2px', background: 'linear-gradient(90deg, transparent, var(--bronze), transparent)' }} />
    </>
  );
}

export default Navbar;
