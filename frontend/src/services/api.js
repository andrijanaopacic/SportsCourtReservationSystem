import axios from 'axios';

const api = axios.create({
    baseURL: 'https://localhost:7070/api'
});

// Sports
export const getSports = (params) => api.get('/sports', { params });
export const getSportById = (id) => api.get(`/sports/${id}`);
export const createSport = (data) => api.post('/sports', data);
export const updateSport = (id, data) => api.put(`/sports/${id}`, data);
export const deleteSport = (id) => api.delete(`/sports/${id}`);

// Courts
export const getCourts = (params) => api.get('/courts', { params });
export const getCourtById = (id) => api.get(`/courts/${id}`);
export const createCourt = (data) => api.post('/courts', data);
export const updateCourt = (id, data) => api.put(`/courts/${id}`, data);
export const deleteCourt = (id) => api.delete(`/courts/${id}`);
export const getCourtsBySport = (sportId) => api.get(`/courts/by-sport/${sportId}`);