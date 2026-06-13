import { Link, useLocation } from 'react-router-dom';

function Navbar() {
  const location = useLocation();
  const isActive = (path) => location.pathname === path || location.pathname.startsWith(path + '/');

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
        <div style={{ display: 'flex', gap: '36px' }}>
          {[{ path: '/', label: 'Sports' }, { path: '/courts', label: 'Courts' }].map(({ path, label }) => (
            <Link key={path} to={path} style={{
              fontFamily: 'Inter, sans-serif', fontSize: '11px',
              letterSpacing: '0.15em', textTransform: 'uppercase',
              color: isActive(path) ? 'var(--cream)' : 'var(--sage)',
              textDecoration: 'none', transition: 'color 0.2s'
            }}>
              {label}
            </Link>
          ))}
        </div>
      </nav>
      <div style={{ height: '2px', background: 'linear-gradient(90deg, transparent, var(--bronze), transparent)' }} />
    </>
  );
}

export default Navbar;