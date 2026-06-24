function Modal({ message, onClose, onConfirm, confirmText = 'Confirm', cancelText = 'Cancel' }) {
  if (!message) return null;

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
      background: 'rgba(78, 34, 15, 0.4)', display: 'flex',
      alignItems: 'center', justifyContent: 'center', zIndex: 1000
    }}>
      <div style={{
        background: 'var(--cream)', padding: '48px',
        maxWidth: '420px', width: '90%',
        borderTop: `2px solid ${onConfirm ? '#991b1b' : 'var(--bronze)'}`,
      }}>
        <p style={{
          fontFamily: 'Cormorant Garamond, serif',
          fontSize: '20px', fontWeight: 300,
          color: 'var(--mahogany)', marginBottom: '32px',
          lineHeight: 1.5
        }}>
          {message}
        </p>

        {onConfirm ? (
          <div style={{ display: 'flex', gap: '12px' }}>
            <button
              className="btn-primary"
              onClick={onClose}
              style={{ flex: 1 }}
            >
              {cancelText}
            </button>
            <button
              onClick={onConfirm}
              style={{
                flex: 1, background: '#991b1b', color: 'var(--cream)',
                border: 'none', padding: '12px 28px',
                fontFamily: 'Inter, sans-serif', fontSize: '11px',
                letterSpacing: '0.12em', textTransform: 'uppercase',
                cursor: 'pointer', transition: 'background 0.2s',
              }}
              onMouseEnter={e => e.target.style.background = '#7f1d1d'}
              onMouseLeave={e => e.target.style.background = '#991b1b'}
            >
              {confirmText}
            </button>
          </div>
        ) : (
          <button className="btn-primary" onClick={onClose}>Close</button>
        )}
      </div>
    </div>
  );
}

export default Modal;
