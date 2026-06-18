import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { createSport, getSportById, updateSport } from '../services/api';

function SportForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = id && id !== 'new';
  const [form, setForm] = useState({ name: '', maxPlayers: '' });
  const [errors, setErrors] = useState([]);

  useEffect(() => {
    if (isEdit) {
      getSportById(id).then(res => setForm({ name: res.data.name, maxPlayers: res.data.maxPlayers }));
    }
  }, [id]);

  const handleSubmit = async () => {
    setErrors([]);
    try {
      const data = { name: form.name, maxPlayers: parseInt(form.maxPlayers) || 0 };
      if (isEdit) { await updateSport(id, data); } else { await createSport(data); }
      navigate('/');
    } catch (err) {
      const data = err.response?.data;
      if (Array.isArray(data)) {
        setErrors(data.map(e => e.errorMessage));
      } else {
        setErrors([data || 'Something went wrong.']);
      }
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">{isEdit ? 'Edit Sport' : 'New Sport'}</div>
          <div className="page-subtitle">{isEdit ? 'Update discipline' : 'Add a discipline'}</div>
        </div>
        <button className="btn-ghost" onClick={() => navigate('/')}>← Back</button>
      </div>
      <div style={{ maxWidth: '480px', display: 'flex', flexDirection: 'column', gap: '32px' }}>
        <div className="form-group">
          <label className="form-label">Sport Name</label>
          <input className="form-input" placeholder="e.g. Tennis"
            value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
        </div>
        <div className="form-group">
          <label className="form-label">Max Players</label>
          <input className="form-input" type="number" placeholder="e.g. 4"
            value={form.maxPlayers} onChange={e => setForm({ ...form, maxPlayers: e.target.value })} />
        </div>
        {errors.length > 0 && (
          <div className="error-msg">
            {errors.map((msg, i) => (
              <div key={i}>{msg}</div>
            ))}
          </div>
        )}
        <div style={{ display: 'flex', gap: '16px' }}>
          <button className="btn-primary" onClick={handleSubmit}>{isEdit ? 'Save Changes' : 'Save Sport'}</button>
          <button className="btn-ghost" onClick={() => navigate('/')}>Cancel</button>
        </div>
      </div>
    </div>
  );
}

export default SportForm;