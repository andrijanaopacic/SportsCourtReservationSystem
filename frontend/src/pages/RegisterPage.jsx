import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { register } from '../services/api';
import { useAuth } from '../context/AuthContext';

function RegisterPage() {
  const { loginUser } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', phoneNumber: '', password: '' });
  const [errors, setErrors] = useState([]);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setLoading(true);
    setErrors([]);
    try {
      const res = await register(form);
      loginUser(res.data.token, {
        email: res.data.email,
        firstName: res.data.firstName,
        lastName: res.data.lastName,
        roles: res.data.roles,
      });
      navigate('/');
    } catch (err) {
      const data = err.response?.data;
      if (Array.isArray(data)) {
        setErrors(data.map(e => e.errorMessage || e));
      } else {
        setErrors([typeof data === 'string' ? data : 'Registration failed.']);
      }
    } finally {
      setLoading(false);
    }
  };

  const fields = [
    { name: 'firstName', label: 'First Name', type: 'text', placeholder: 'Marija' },
    { name: 'lastName', label: 'Last Name', type: 'text', placeholder: 'Petrović' },
    { name: 'email', label: 'Email', type: 'email', placeholder: 'marija@gmail.com' },
    { name: 'phoneNumber', label: 'Phone', type: 'tel', placeholder: '0641234567' },
    { name: 'password', label: 'Password', type: 'password', placeholder: '••••••••' },
  ];

  return (
    <div className="page" style={{ display: 'flex', justifyContent: 'center', paddingTop: '80px' }}>
      <div style={{ width: '100%', maxWidth: '420px' }}>
        <div className="page-header" style={{ borderBottom: '1px solid var(--sage)', marginBottom: '40px', paddingBottom: '20px' }}>
          <div>
            <div className="page-title">Register</div>
            <div className="page-subtitle">Create your account</div>
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          {fields.map(({ name, label, type, placeholder }) => (
            <div key={name} className="form-group">
              <label className="form-label">{label}</label>
              <input
                className="form-input"
                type={type}
                placeholder={placeholder}
                value={form[name]}
                onChange={e => setForm({ ...form, [name]: e.target.value })}
              />
            </div>
          ))}

          {errors.length > 0 && (
            <div className="error-msg">
              {errors.map((e, i) => <div key={i}>{e}</div>)}
            </div>
          )}

          <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
            <button className="btn-primary" onClick={handleSubmit} disabled={loading}>
              {loading ? 'Registering...' : 'Create Account'}
            </button>
            <Link to="/login" style={{ fontSize: '11px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', textDecoration: 'none' }}>
              Have account? Sign in →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

export default RegisterPage;
