import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getCourts, deleteCourt } from '../services/api';

function CourtsList() {
  const [courts, setCourts] = useState([]);
  const [name, setName] = useState('');
  const [isIndoor, setIsIndoor] = useState('');
  const [error, setError] = useState('');

  useEffect(() => { fetchCourts(); }, []);

  const fetchCourts = async () => {
    try {
      const params = {};
      if (name) params.name = name;
      if (isIndoor !== '') params.isIndoor = isIndoor;
      const res = await getCourts(params);
      setCourts(res.data);
    } catch {
      setError('Failed to load courts.');
    }
  };

  const handleShowAll = async () => {
    setName('');
    setIsIndoor('');
    try {
      const res = await getCourts({});
      setCourts(res.data);
    } catch {
      setError('Failed to load courts.');
    }
  };

  const handleDelete = async (id) => {
    try {
      await deleteCourt(id);
      fetchCourts();
    } catch (err) {
      alert(err.response?.data || 'Cannot delete court.');
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">Courts</div>
          <div className="page-subtitle">Browse & reserve</div>
        </div>
        <Link to="/courts/new">
          <button className="btn-primary">+ Add Court</button>
        </Link>
      </div>

      <div className="filter-bar">
        <div className="form-group">
          <label className="form-label">Search</label>
          <input
            className="form-input"
            placeholder="Court name..."
            value={name}
            onChange={e => setName(e.target.value)}
            style={{ width: '220px' }}
          />
        </div>
        <div className="form-group">
          <label className="form-label">Type</label>
          <select
            className="form-select"
            value={isIndoor}
            onChange={e => setIsIndoor(e.target.value)}
            style={{ width: '160px' }}
          >
            <option value="">All types</option>
            <option value="true">Indoor</option>
            <option value="false">Outdoor</option>
          </select>
        </div>
        <button className="btn-ghost" onClick={fetchCourts}>Search</button>
        <button className="btn-ghost" onClick={handleShowAll}>Show All</button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      <table>
        <thead>
          <tr>
            <th>Court</th>
            <th>Sport</th>
            <th>Location</th>
            <th>Price / hr</th>
            <th>Type</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {courts.map(court => (
            <tr key={court.courtId}>
              <td className="td-name">{court.name}</td>
              <td>{court.sportName}</td>
              <td>{court.location}</td>
              <td>{court.pricePerHour} RSD</td>
              <td>
                <span className={`badge ${court.isIndoor ? 'badge-indoor' : 'badge-outdoor'}`}>
                  {court.isIndoor ? 'Indoor' : 'Outdoor'}
                </span>
              </td>
              <td className="actions">
                <Link to={`/courts/${court.courtId}`}>
                  <button className="btn-ghost">Edit</button>
                </Link>
                <span className="divider">|</span>
                <button className="btn-danger" onClick={() => handleDelete(court.courtId)}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default CourtsList;