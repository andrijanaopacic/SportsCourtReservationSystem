import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { login } from '../services/api';
import { useAuth } from '../context/AuthContext';

function LoginPage() {
  const { loginUser } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await login(form);
      loginUser(res.data.token, {
        email: res.data.email,
        firstName: res.data.firstName,
        lastName: res.data.lastName,
        roles: res.data.roles,
      });
      navigate('/');
    } catch (err) {
      setError(err.response?.data || 'Pogrešan email ili lozinka.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page" style={{ display: 'flex', justifyContent: 'center', paddingTop: '80px' }}>
      <div style={{ width: '100%', maxWidth: '420px' }}>
        <div className="page-header" style={{ borderBottom: '1px solid var(--sage)', marginBottom: '40px', paddingBottom: '20px' }}>
          <div>
            <div className="page-title">Sign In</div>
            <div className="page-subtitle">Welcome back</div>
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
          <div className="form-group">
            <label className="form-label">Email</label>
            <input
              className="form-input"
              type="email"
              placeholder="marija@gmail.com"
              value={form.email}
              onChange={e => setForm({ ...form, email: e.target.value })}
            />
          </div>

          <div className="form-group">
            <label className="form-label">Password</label>
            <input
              className="form-input"
              type="password"
              placeholder="••••••••"
              value={form.password}
              onChange={e => setForm({ ...form, password: e.target.value })}
            />
          </div>

          {error && <div className="error-msg">{error}</div>}

          <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
            <button className="btn-primary" onClick={handleSubmit} disabled={loading}>
              {loading ? 'Signing in...' : 'Sign In'}
            </button>
            <Link to="/register" style={{ fontSize: '11px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', textDecoration: 'none' }}>
              No account? Register →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

export default LoginPage;
