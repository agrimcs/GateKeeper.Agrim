import api from './api';

const authService = {
  /**
   * Register a new user
   */
  async register(email, password, confirmPassword, firstName, lastName) {
    const response = await api.post('/api/auth/register', {
      email,
      password,
      confirmPassword,
      firstName,
      lastName,
    });
    return response.data;
  },

  /**
   * Login user
   */
  async login(email, password) {
    const response = await api.post('/api/auth/login', {
      email,
      password,
    });
    
    // Store token if returned (check both possible field names)
    const token = response.data.token || response.data.Token || response.data.accessToken;
    if (token) {
      localStorage.setItem('access_token', token);
    }
    
    return response.data;
  },

  /**
   * Logout user
   */
  logout() {
    localStorage.removeItem('access_token');
  },

  /**
   * Get current user profile
   */
  async getProfile(userId) {
    const response = await api.get(`/api/auth/profile/${userId}`);
    return response.data;
  },

  /**
   * Check if user is authenticated
   */
  isAuthenticated() {
    return !!localStorage.getItem('access_token');
  },
};

export default authService;
