import { useState, useEffect, Fragment } from 'react';
import { Link } from 'react-router-dom';
import { getCourts, deleteCourt } from '../services/api';
import Modal from '../components/Modal';

function CourtsReserve() {
  const [courts, setCourts] = useState([]);
  const [name, setName] = useState('');
  const [isIndoor, setIsIndoor] = useState('');
  const [error, setError] = useState('');
  const [modalMessage, setModalMessage] = useState('');
  const [expandedDescriptions, setExpandedDescriptions] = useState({});

  const fetchCourts = async () => {
    try {
      const params = {};

      if (name) params.name = name;
      if (isIndoor !== '') params.isIndoor = isIndoor;

      const res = await getCourts(params);
      setCourts(res.data);
      setError('');
    } catch {
      setError('Failed to load courts.');
    }
  };

  // samo INITIAL load (bez dependencies → nema warninga)
  useEffect(() => {
    fetchCourts();
  }, []);

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



  const toggleDescription = (id) => {
    setExpandedDescriptions(prev => ({
      ...prev,
      [id]: !prev[id]
    }));
  };

  return (
    <div className="page">
      <Modal message={modalMessage} onClose={() => setModalMessage('')} />

      <div className="page-header">
        <div>
          <div className="page-title">Reserve Courts</div>
          <div className="page-subtitle">Browse & reserve</div>
        </div>

       
      </div>

      <div className="filter-bar">
        <input
          className="form-input"
          placeholder="Court name..."
          value={name}
          onChange={(e) => setName(e.target.value)}
        />

        <select
          className="form-select"
          value={isIndoor}
          onChange={(e) => setIsIndoor(e.target.value)}
        >
          <option value="">All types</option>
          <option value="true">Indoor</option>
          <option value="false">Outdoor</option>
        </select>

        <button className="btn-ghost" onClick={fetchCourts}>
          Search
        </button>

        <button className="btn-ghost" onClick={handleShowAll}>
          Show All
        </button>
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
          {courts.map((court) => (
            <Fragment key={court.courtId}>
              <tr>
                <td>{court.name}</td>
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
                        onClick={() => toggleDescription(court.courtId)}
                        className="btn-ghost"
                      >
                        {expandedDescriptions[court.courtId] ? '▲ less' : '▼ more'}
                      </button>

                      <span className="divider">|</span>
                    </>
                  )}

                  <Link
                    to={`/courts/${court.courtId}/calendar`}
                    className="btn-ghost"
                  >
                    Calendar
                  </Link>
<Link to={`/courts/${court.courtId}/reserve`}>
  <button className="btn-primary">Reserve</button>
</Link>
                  <span className="divider">|</span>
 
                </td>
              </tr>

              {expandedDescriptions[court.courtId] && court.description && (
                <tr>
                  <td
                    colSpan="6"
                    style={{
                      padding: '0 0 16px 0',
                      fontSize: '13px',
                      color: 'var(--bronze)',
                      fontStyle: 'italic'
                    }}
                  >
                    {court.description}
                  </td>
                </tr>
              )}
            </Fragment>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default CourtsReserve;