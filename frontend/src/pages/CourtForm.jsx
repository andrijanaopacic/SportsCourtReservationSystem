import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { createCourt, getCourtById, updateCourt, getSports } from '../services/api';

function CourtForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = id && id !== 'new';

  const [form, setForm] = useState({
    name: '',
    location: '',
    description: '',
    pricePerHour: '',
    isIndoor: false,
    sportId: ''
  });
  const [sports, setSports] = useState([]);
  const [errors, setErrors] = useState([]);

  useEffect(() => {
    getSports({}).then(res => setSports(res.data));

    if (isEdit) {
      getCourtById(id).then(res => {
        const c = res.data;
        setForm({
          name: c.name,
          location: c.location,
          description: c.description,
          pricePerHour: c.pricePerHour,
          isIndoor: c.isIndoor,
          sportId: c.sportId ? String(c.sportId) : ''
        });
      });
    }
  }, [id]);

  const handleSubmit = async () => {
    setErrors([]);

    try {
      const data = {
        name: form.name,
        location: form.location,
        description: form.description || '',
        pricePerHour: parseFloat(form.pricePerHour) || 0,
        isIndoor: form.isIndoor,
        sportId: parseInt(form.sportId) || 0
      };
      if (isEdit) {
        await updateCourt(id, data);
      } else {
        await createCourt(data);
      }
      navigate('/courts');
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
          <div className="page-title">{isEdit ? 'Edit Court' : 'New Court'}</div>
          <div className="page-subtitle">{isEdit ? 'Update court details' : 'Add a new court'}</div>
        </div>
        <button className="btn-ghost" onClick={() => navigate('/courts')}>← Back</button>
      </div>

      <div style={{ maxWidth: '480px', display: 'flex', flexDirection: 'column', gap: '24px' }}>
        <div className="form-group">
          <label className="form-label">Court Name</label>
          <input
            className="form-input"
            placeholder="e.g. Center Court"
            value={form.name}
            onChange={e => setForm({ ...form, name: e.target.value })}
          />
        </div>

        <div className="form-group">
          <label className="form-label">Location</label>
          <input
            className="form-input"
            placeholder="e.g. Novi Sad, Hall A"
            value={form.location}
            onChange={e => setForm({ ...form, location: e.target.value })}
          />
        </div>

        <div className="form-group">
          <label className="form-label">
            Description{' '}
            <span style={{ color: 'var(--sage)', fontSize: '10px', letterSpacing: '0.05em' }}>optional</span>
          </label>
          <input
            className="form-input"
            placeholder="Optional description"
            value={form.description}
            onChange={e => setForm({ ...form, description: e.target.value })}
          />
        </div>

        <div className="form-group">
          <label className="form-label">Price per Hour (RSD)</label>
          <input
            className="form-input"
            type="number"
            placeholder="e.g. 1200"
            value={form.pricePerHour}
            onChange={e => setForm({ ...form, pricePerHour: e.target.value })}
          />
        </div>

        <div className="form-group">
          <label className="form-label">Sport</label>
          <select
            className="form-select"
            value={form.sportId}
            onChange={e => setForm({ ...form, sportId: e.target.value })}
          >
            <option value="" disabled>-- Select sport --</option>
            {sports.length === 0 ? (
              <option value="" disabled>No sports available. Please create a sport first.</option>
            ) : (
              sports.map(s => (
                <option key={s.sportId} value={s.sportId}>{s.name}</option>
              ))
            )}
          </select>
        </div>

        <div className="form-group">
          <label className="form-label" style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={form.isIndoor}
              onChange={e => setForm({ ...form, isIndoor: e.target.checked })}
            />
            Indoor court
          </label>
        </div>

        {errors.length > 0 && (
          <div className="error-msg">
            {errors.map((msg, i) => (
              <div key={i}>{msg}</div>
            ))}
          </div>
        )}

        <div style={{ display: 'flex', gap: '16px' }}>
          <button className="btn-primary" onClick={handleSubmit}>
            {isEdit ? 'Save Changes' : 'Save Court'}
          </button>
          <button className="btn-ghost" onClick={() => navigate('/courts')}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}

export default CourtForm;