import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import clientService from '../../services/clientService';

const ClientFormPage = () => {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    displayName: '',
    type: 'Public',
    redirectUris: '',
    allowedScopes: 'openid profile email',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [clientSecret, setClientSecret] = useState('');

  useEffect(() => {
    if (isEdit) {
      loadClient();
    }
  }, [id]);

  const loadClient = async () => {
    try {
      const data = await clientService.getById(id);
      setFormData({
        displayName: data.displayName,
        type: data.type,
        redirectUris: data.redirectUris?.join('\n') || '',
        allowedScopes: data.allowedScopes?.join(' ') || '',
      });
    } catch (err) {
      setError(err.message || 'Failed to load client');
    }
  };

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    // Convert type string to enum numeric value (Public=0, Confidential=1)
    const typeValue = formData.type === 'Confidential' ? 1 : 0;

    const payload = {
      DisplayName: formData.displayName,
      Type: typeValue,
      RedirectUris: formData.redirectUris
        .split('\n')
        .map((uri) => uri.trim())
        .filter((uri) => uri),
      AllowedScopes: formData.allowedScopes
        .split(' ')
        .map((scope) => scope.trim())
        .filter((scope) => scope),
    };

    try {
      if (isEdit) {
        await clientService.update(id, payload);
        navigate('/admin/clients');
      } else {
        const response = await clientService.create(payload);
        
        // If confidential client, show the secret
        if (response.clientSecret) {
          setClientSecret(response.clientSecret);
        } else {
          navigate('/admin/clients');
        }
      }
    } catch (err) {
      setError(err.message || 'Failed to save client');
    } finally {
      setLoading(false);
    }
  };

  // Show secret dialog after creation
  if (clientSecret) {
    return (
      <div className="max-w-2xl mx-auto">
        <div className="bg-white rounded-lg shadow-lg p-8">
          <h2 className="text-2xl font-bold mb-4 text-green-600">
            Client Created Successfully!
          </h2>
          
          <div className="bg-yellow-50 border-l-4 border-yellow-400 p-4 mb-6">
            <p className="text-sm text-yellow-700">
              <strong>Important:</strong> Save this client secret now. You won't be able to see it again!
            </p>
          </div>

          <div className="mb-6">
            <label className="block text-gray-700 font-medium mb-2">Client Secret:</label>
            <div className="bg-gray-100 p-4 rounded border border-gray-300 font-mono text-sm break-all">
              {clientSecret}
            </div>
          </div>

          <button
            onClick={() => navigate('/admin/clients')}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700"
          >
            Continue to Client List
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto">
      <h1 className="text-3xl font-bold mb-6">
        {isEdit ? 'Edit OAuth Client' : 'Create OAuth Client'}
      </h1>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="bg-white rounded-lg shadow p-6">
        <div className="mb-4">
          <label htmlFor="displayName" className="block text-gray-700 font-medium mb-2">
            Display Name *
          </label>
          <input
            id="displayName"
            name="displayName"
            type="text"
            value={formData.displayName}
            onChange={handleChange}
            required
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="My Application"
          />
        </div>

        <div className="mb-4">
          <label htmlFor="type" className="block text-gray-700 font-medium mb-2">
            Client Type *
          </label>
          <select
            id="type"
            name="type"
            value={formData.type}
            onChange={handleChange}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="Public">Public (SPA, Mobile App)</option>
            <option value="Confidential">Confidential (Web Server)</option>
          </select>
          <p className="text-sm text-gray-500 mt-1">
            Public clients use PKCE. Confidential clients use client secret.
          </p>
        </div>

        <div className="mb-4">
          <label htmlFor="redirectUris" className="block text-gray-700 font-medium mb-2">
            Redirect URIs * (one per line)
          </label>
          <textarea
            id="redirectUris"
            name="redirectUris"
            value={formData.redirectUris}
            onChange={handleChange}
            required
            rows="4"
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="http://localhost:5173/callback
https://myapp.com/oauth/callback"
          />
        </div>

        <div className="mb-6">
          <label htmlFor="allowedScopes" className="block text-gray-700 font-medium mb-2">
            Allowed Scopes (space-separated)
          </label>
          <input
            id="allowedScopes"
            name="allowedScopes"
            type="text"
            value={formData.allowedScopes}
            onChange={handleChange}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="openid profile email"
          />
        </div>

        <div className="flex gap-4">
          <button
            type="submit"
            disabled={loading}
            className="flex-1 bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700 disabled:bg-gray-400"
          >
            {loading ? 'Saving...' : isEdit ? 'Update Client' : 'Create Client'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/admin/clients')}
            className="flex-1 bg-gray-300 text-gray-700 py-2 px-4 rounded hover:bg-gray-400"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
};

export default ClientFormPage;
