import api from './api';

const clientService = {
  /**
   * Get all OAuth clients
   */
  async getAll(skip = 0, take = 50) {
    const response = await api.get('/api/clients', {
      params: { skip, take },
    });
    return response.data;
  },

  /**
   * Get client by ID
   */
  async getById(id) {
    const response = await api.get(`/api/clients/${id}`);
    return response.data;
  },

  /**
   * Create new OAuth client
   */
  async create(clientData) {
    const response = await api.post('/api/clients', clientData);
    return response.data;
  },

  /**
   * Update OAuth client
   */
  async update(id, clientData) {
    const response = await api.put(`/api/clients/${id}`, clientData);
    return response.data;
  },

  /**
   * Delete OAuth client
   */
  async delete(id) {
    await api.delete(`/api/clients/${id}`);
  },
};

export default clientService;
