import axios from 'axios';
import { getTenantFromHost } from './tenant';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5294';

console.log('API Base URL:', API_BASE_URL); // Debug log

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Required for cookies and CORS with credentials
});

// Request interceptor (add auth token if available)
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('access_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // Add tenant header and query param if present
    const tenant = getTenantFromHost();
    if (tenant) {
      config.headers['X-Tenant'] = tenant;
      // Add as query param for localhost (middleware checks this)
      config.params = config.params || {};
      config.params.tenant = tenant;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor (handle errors globally)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      // Server responded with error status
      const { status, data } = error.response;
      
      if (status === 401) {
        // Unauthorized - clear token and redirect to login
        localStorage.removeItem('access_token');
        window.location.href = '/login';
      }
      
      // Return formatted error
      return Promise.reject({
        status,
        message: data.error || data.detail || data.message || data.title || 'An error occurred',
        errors: data.errors || {},
      });
    } else if (error.request) {
      // Request made but no response
      return Promise.reject({
        message: 'Network error - please check your connection',
      });
    } else {
      // Something else happened
      return Promise.reject({
        message: error.message || 'An unexpected error occurred',
      });
    }
  }
);

export default api;
