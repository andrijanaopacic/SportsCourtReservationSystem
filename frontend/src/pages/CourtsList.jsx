import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getCourts, deleteCourt, getSports } from '../services/api';
import Modal from '../components/Modal';
import React from 'react';

function CourtsList() {
  const [courts, setCourts] = useState([]);
  const [name, setName] = useState('');
  const [isIndoor, setIsIndoor] = useState('');
  const [sportId, setSportId] = useState('');
  const [sports, setSports] = useState([]);
  const [error, setError] = useState('');
  const [modalMessage, setModalMessage] = useState('');
  const [expandedDescriptions, setExpandedDescriptions] = useState({});

  useEffect(() => {
    fetchCourts();
    getSports({}).then(res => setSports(res.data));
  }, []);

  const fetchCourts = async () => {
    try {
      const params = {};
      if (name) params.name = name;
      if (isIndoor !== '') params.isIndoor = isIndoor;
      if (sportId) params.sportId = sportId;
      const res = await getCourts(params);
      setCourts(res.data);
    } catch {
      setError('Failed to load courts.');
    }
  };

  const handleShowAll = async () => {
    setName('');
    setIsIndoor('');
    setSportId('');
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
      setModalMessage(err.response?.data || 'Cannot delete court.');
    }
  };

  const toggleDescription = (id) => {
    setExpandedDescriptions(prev => ({ ...prev, [id]: !prev[id] }));
  };

  return (
    <div className="page">
      <Modal message={modalMessage} onClose={() => setModalMessage('')} />

      <div className="page-header">
        <div>
          <div className="page-title">Courts</div>
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
        <div className="form-group">
          <label className="form-label">Sport</label>
          <select
            className="form-select"
            value={sportId}
            onChange={e => setSportId(e.target.value)}
            style={{ width: '160px' }}
          >
            <option value="">All sports</option>
            {sports.map(s => (
              <option key={s.sportId} value={s.sportId}>{s.name}</option>
            ))}
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
            <React.Fragment key={court.courtId}>
              <tr>
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
                  {court.description && (
                    <>
                      <button
                        style={{
                          background: 'none', border: 'none',
                          color: 'var(--sage)', fontSize: '11px',
                          letterSpacing: '0.1em', textTransform: 'uppercase',
                          cursor: 'pointer', padding: '8px 0'
                        }}
                        onClick={() => toggleDescription(court.courtId)}
                      >
                        {expandedDescriptions[court.courtId] ? '▲ less' : '▼ more'}
                      </button>
                      <span className="divider">|</span>
                    </>
                  )}
                  <Link to={`/courts/${court.courtId}`}>
                    <button className="btn-ghost">Edit</button>
                  </Link>
                  <span className="divider">|</span>
                  <button className="btn-danger" onClick={() => handleDelete(court.courtId)}>
                    Delete
                  </button>
                </td>
              </tr>
              {expandedDescriptions[court.courtId] && court.description && (
                <tr>
                  <td colSpan="6" style={{
                    padding: '0 0 16px 0',
                    fontSize: '13px',
                    color: 'var(--bronze)',
                    fontWeight: 300,
                    fontStyle: 'italic',
                    letterSpacing: '0.02em'
                  }}>
                    {court.description}
                  </td>
                </tr>
              )}
            </React.Fragment>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default CourtsList;