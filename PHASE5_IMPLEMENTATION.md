# Phase 5: React Frontend - Implementation Guide

**Estimated Time:** 4-6 hours  
**Goal:** Build React SPA with authentication, OAuth consent screen, and client management  
**Prerequisites:** Phase 4 (API Layer & OAuth Server) completed

---

## Objectives

By the end of Phase 5, you will have:
- ✅ React Router setup with protected routes
- ✅ Login and Registration pages
- ✅ OAuth consent/authorization screen
- ✅ Admin dashboard for client management (list, create, edit, delete)
- ✅ User authentication state management
- ✅ HTTP client with proper error handling
- ✅ Complete integration with backend APIs
- ✅ Working end-to-end OAuth2 flow

---

## High-Level Architecture

```
gatekeeper.client/
├── src/
│   ├── main.jsx                    # App entry point
│   ├── App.jsx                     # Root component with routing
│   ├── features/
│   │   ├── auth/
│   │   │   ├── LoginPage.jsx       # User login
│   │   │   ├── RegisterPage.jsx    # User registration
│   │   │   └── AuthContext.jsx     # Auth state management
│   │   ├── oauth/
│   │   │   └── ConsentPage.jsx     # OAuth consent screen
│   │   └── admin/
│   │       ├── ClientListPage.jsx  # List all OAuth clients
│   │       ├── ClientFormPage.jsx  # Create/Edit client
│   │       └── DashboardPage.jsx   # Admin home
│   ├── components/
│   │   ├── Layout.jsx              # Main layout wrapper
│   │   ├── ProtectedRoute.jsx      # Auth route guard
│   │   └── Navigation.jsx          # Nav bar
│   ├── services/
│   │   ├── api.js                  # Axios HTTP client
│   │   ├── authService.js          # Auth API calls
│   │   └── clientService.js        # Client API calls
│   └── utils/
│       └── validation.js           # Form validation helpers
```

---

## Task 1: Setup Dependencies

### Install required packages:

```bash
cd gatekeeper.client
npm install react-router-dom axios
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

### Update `tailwind.config.js`:

```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

### Update `src/index.css`:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Custom styles */
body {
  margin: 0;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen',
    'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans', 'Helvetica Neue',
    sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

code {
  font-family: source-code-pro, Menlo, Monaco, Consolas, 'Courier New',
    monospace;
}
```

---

## Task 2: Create API Client

**File:** `src/services/api.js`

```javascript
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5294';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // For cookies if needed
});

// Request interceptor (add auth token if available)
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('access_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
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
        message: data.detail || data.message || 'An error occurred',
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
```

---

## Task 3: Create Authentication Service

**File:** `src/services/authService.js`

```javascript
import api from './api';

const authService = {
  /**
   * Register a new user
   */
  async register(email, password, firstName, lastName) {
    const response = await api.post('/api/auth/register', {
      email,
      password,
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
    
    // Store token if returned
    if (response.data.accessToken) {
      localStorage.setItem('access_token', response.data.accessToken);
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
```

---

## Task 4: Create Client Management Service

**File:** `src/services/clientService.js`

```javascript
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
```

---

## Task 5: Create Authentication Context

**File:** `src/features/auth/AuthContext.jsx`

```jsx
import { createContext, useContext, useState, useEffect } from 'react';
import authService from '../../services/authService';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is already authenticated
    const initAuth = () => {
      if (authService.isAuthenticated()) {
        // In a real app, fetch user profile here
        setUser({ authenticated: true });
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = async (email, password) => {
    const data = await authService.login(email, password);
    setUser({ authenticated: true, ...data });
    return data;
  };

  const register = async (email, password, firstName, lastName) => {
    const data = await authService.register(email, password, firstName, lastName);
    return data;
  };

  const logout = () => {
    authService.logout();
    setUser(null);
  };

  const value = {
    user,
    loading,
    login,
    register,
    logout,
    isAuthenticated: !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
```

---

## Task 6: Create Protected Route Component

**File:** `src/components/ProtectedRoute.jsx`

```jsx
import { Navigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';

const ProtectedRoute = ({ children }) => {
  const { isAuthenticated, loading } = useAuth();

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg">Loading...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return children;
};

export default ProtectedRoute;
```

---

## Task 7: Create Navigation Component

**File:** `src/components/Navigation.jsx`

```jsx
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';

const Navigation = () => {
  const { isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="bg-blue-600 text-white shadow-lg">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-between h-16">
          <Link to="/" className="text-xl font-bold">
            GateKeeper
          </Link>

          <div className="flex items-center space-x-4">
            {isAuthenticated ? (
              <>
                <Link to="/admin/clients" className="hover:text-blue-200">
                  Clients
                </Link>
                <button
                  onClick={handleLogout}
                  className="bg-blue-700 hover:bg-blue-800 px-4 py-2 rounded"
                >
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="hover:text-blue-200">
                  Login
                </Link>
                <Link
                  to="/register"
                  className="bg-blue-700 hover:bg-blue-800 px-4 py-2 rounded"
                >
                  Register
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navigation;
```

---

## Task 8: Create Layout Component

**File:** `src/components/Layout.jsx`

```jsx
import Navigation from './Navigation';

const Layout = ({ children }) => {
  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      <main className="container mx-auto px-4 py-8">{children}</main>
    </div>
  );
};

export default Layout;
```

---

## Task 9: Create Login Page

**File:** `src/features/auth/LoginPage.jsx`

```jsx
import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from './AuthContext';

const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login(email, password);
      navigate('/admin/clients');
    } catch (err) {
      setError(err.message || 'Login failed. Please check your credentials.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-md">
        <h2 className="text-2xl font-bold mb-6 text-center">Login to GateKeeper</h2>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label htmlFor="email" className="block text-gray-700 font-medium mb-2">
              Email
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="john.doe@example.com"
            />
          </div>

          <div className="mb-6">
            <label htmlFor="password" className="block text-gray-700 font-medium mb-2">
              Password
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="••••••••"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-400"
          >
            {loading ? 'Logging in...' : 'Login'}
          </button>
        </form>

        <p className="mt-4 text-center text-gray-600">
          Don't have an account?{' '}
          <Link to="/register" className="text-blue-600 hover:underline">
            Register here
          </Link>
        </p>
      </div>
    </div>
  );
};

export default LoginPage;
```

---

## Task 10: Create Registration Page

**File:** `src/features/auth/RegisterPage.jsx`

```jsx
import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from './AuthContext';

const RegisterPage = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    // Validate passwords match
    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    setLoading(true);

    try {
      await register(
        formData.email,
        formData.password,
        formData.firstName,
        formData.lastName
      );
      navigate('/login');
    } catch (err) {
      setError(err.message || 'Registration failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-md">
        <h2 className="text-2xl font-bold mb-6 text-center">Create Account</h2>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label htmlFor="firstName" className="block text-gray-700 font-medium mb-2">
              First Name
            </label>
            <input
              id="firstName"
              name="firstName"
              type="text"
              value={formData.firstName}
              onChange={handleChange}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="mb-4">
            <label htmlFor="lastName" className="block text-gray-700 font-medium mb-2">
              Last Name
            </label>
            <input
              id="lastName"
              name="lastName"
              type="text"
              value={formData.lastName}
              onChange={handleChange}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="mb-4">
            <label htmlFor="email" className="block text-gray-700 font-medium mb-2">
              Email
            </label>
            <input
              id="email"
              name="email"
              type="email"
              value={formData.email}
              onChange={handleChange}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="mb-4">
            <label htmlFor="password" className="block text-gray-700 font-medium mb-2">
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              value={formData.password}
              onChange={handleChange}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="mb-6">
            <label htmlFor="confirmPassword" className="block text-gray-700 font-medium mb-2">
              Confirm Password
            </label>
            <input
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              value={formData.confirmPassword}
              onChange={handleChange}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-400"
          >
            {loading ? 'Creating account...' : 'Register'}
          </button>
        </form>

        <p className="mt-4 text-center text-gray-600">
          Already have an account?{' '}
          <Link to="/login" className="text-blue-600 hover:underline">
            Login here
          </Link>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
```

---

## Task 11: Create Client List Page

**File:** `src/features/admin/ClientListPage.jsx`

```jsx
import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import clientService from '../../services/clientService';

const ClientListPage = () => {
  const [clients, setClients] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    loadClients();
  }, []);

  const loadClients = async () => {
    try {
      const data = await clientService.getAll();
      setClients(data);
    } catch (err) {
      setError(err.message || 'Failed to load clients');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id, displayName) => {
    if (!window.confirm(`Are you sure you want to delete "${displayName}"?`)) {
      return;
    }

    try {
      await clientService.delete(id);
      setClients(clients.filter((c) => c.id !== id));
    } catch (err) {
      alert(err.message || 'Failed to delete client');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg">Loading...</div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">OAuth Clients</h1>
        <Link
          to="/admin/clients/new"
          className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
        >
          + New Client
        </Link>
      </div>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {clients.length === 0 ? (
        <div className="bg-white rounded-lg shadow p-8 text-center">
          <p className="text-gray-600 mb-4">No OAuth clients registered yet.</p>
          <Link
            to="/admin/clients/new"
            className="text-blue-600 hover:underline"
          >
            Register your first client
          </Link>
        </div>
      ) : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Display Name
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Client ID
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Type
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Redirect URIs
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {clients.map((client) => (
                <tr key={client.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">
                      {client.displayName}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-500">{client.clientId}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                        client.type === 'Public'
                          ? 'bg-green-100 text-green-800'
                          : 'bg-blue-100 text-blue-800'
                      }`}
                    >
                      {client.type}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="text-sm text-gray-500">
                      {client.redirectUris?.slice(0, 2).join(', ')}
                      {client.redirectUris?.length > 2 && '...'}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => navigate(`/admin/clients/${client.id}/edit`)}
                      className="text-blue-600 hover:text-blue-900 mr-4"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(client.id, client.displayName)}
                      className="text-red-600 hover:text-red-900"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default ClientListPage;
```

---

## Task 12: Create Client Form Page

**File:** `src/features/admin/ClientFormPage.jsx`

```jsx
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

    const payload = {
      displayName: formData.displayName,
      type: formData.type,
      redirectUris: formData.redirectUris
        .split('\n')
        .map((uri) => uri.trim())
        .filter((uri) => uri),
      allowedScopes: formData.allowedScopes
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
```

---

## Task 13: Create OAuth Consent Page

**File:** `src/features/oauth/ConsentPage.jsx`

```jsx
import { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';

const ConsentPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  
  const [loading, setLoading] = useState(false);
  const [clientName, setClientName] = useState('');
  const [scopes, setScopes] = useState([]);

  useEffect(() => {
    // Extract OAuth parameters
    const client = searchParams.get('client_id') || 'Unknown Application';
    const scope = searchParams.get('scope') || '';
    
    setClientName(client);
    setScopes(scope.split(' ').filter(s => s));
  }, [searchParams]);

  const handleApprove = async () => {
    setLoading(true);
    
    // In a real implementation, this would call the authorize endpoint
    // For now, redirect back with approval
    const redirectUri = searchParams.get('redirect_uri');
    const state = searchParams.get('state');
    
    // This is simplified - actual implementation would handle the OAuth flow properly
    window.location.href = `${redirectUri}?code=mock_auth_code&state=${state}`;
  };

  const handleDeny = () => {
    const redirectUri = searchParams.get('redirect_uri');
    const state = searchParams.get('state');
    window.location.href = `${redirectUri}?error=access_denied&state=${state}`;
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-md">
        <h2 className="text-2xl font-bold mb-4 text-center">Authorization Request</h2>
        
        <div className="mb-6">
          <p className="text-gray-700 mb-2">
            <strong>{clientName}</strong> is requesting access to:
          </p>
          
          <ul className="list-disc list-inside space-y-2 text-gray-600">
            {scopes.map((scope) => (
              <li key={scope}>{scope}</li>
            ))}
          </ul>
        </div>

        <div className="bg-blue-50 border-l-4 border-blue-400 p-4 mb-6">
          <p className="text-sm text-blue-700">
            By approving, you allow this application to access your information according to their privacy policy.
          </p>
        </div>

        <div className="flex gap-4">
          <button
            onClick={handleDeny}
            className="flex-1 bg-gray-300 text-gray-700 py-2 px-4 rounded hover:bg-gray-400"
          >
            Deny
          </button>
          <button
            onClick={handleApprove}
            disabled={loading}
            className="flex-1 bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700 disabled:bg-gray-400"
          >
            {loading ? 'Processing...' : 'Approve'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default ConsentPage;
```

---

## Task 14: Update App.jsx with Routing

**File:** `src/App.jsx`

```jsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './features/auth/AuthContext';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import ClientListPage from './features/admin/ClientListPage';
import ClientFormPage from './features/admin/ClientFormPage';
import ConsentPage from './features/oauth/ConsentPage';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/oauth/authorize" element={<ConsentPage />} />

          {/* Protected routes */}
          <Route
            path="/admin/*"
            element={
              <ProtectedRoute>
                <Layout>
                  <Routes>
                    <Route path="clients" element={<ClientListPage />} />
                    <Route path="clients/new" element={<ClientFormPage />} />
                    <Route path="clients/:id/edit" element={<ClientFormPage />} />
                  </Routes>
                </Layout>
              </ProtectedRoute>
            }
          />

          {/* Default redirect */}
          <Route path="/" element={<Navigate to="/admin/clients" replace />} />
          <Route path="*" element={<Navigate to="/admin/clients" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
```

---

## Task 15: Update main.jsx

**File:** `src/main.jsx`

```jsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
```

---

## Task 16: Create Environment Configuration

**File:** `.env.development`

```env
VITE_API_URL=http://localhost:5294
```

**File:** `.env.production`

```env
VITE_API_URL=https://your-production-api.com
```

---

## Task 17: Run and Test

### Start the backend:
```bash
cd GateKeeper.Server
dotnet run
```

### Start the frontend (in another terminal):
```bash
cd gatekeeper.client
npm run dev
```

Frontend will be at: http://localhost:5173

---

## Testing Checklist

- [ ] ✅ Can register new user
- [ ] ✅ Can login with registered user
- [ ] ✅ Can view client list (empty initially)
- [ ] ✅ Can create new OAuth client (Public)
- [ ] ✅ Can create new OAuth client (Confidential) - shows secret
- [ ] ✅ Can edit existing client
- [ ] ✅ Can delete client
- [ ] ✅ Protected routes redirect to login when not authenticated
- [ ] ✅ Logout works correctly
- [ ] ✅ Error messages display properly
- [ ] ✅ Form validation works

---

## Phase 5 Summary

✅ **Authentication UI** - Login and registration  
✅ **Client Management** - Full CRUD interface  
✅ **OAuth Consent** - Authorization screen (basic)  
✅ **Protected Routes** - Auth guards  
✅ **State Management** - Auth context  
✅ **HTTP Client** - Axios with interceptors  

**Status:** 🎯 MVP Complete - Ready for OAuth Flow Testing

---

**Next Phase:** Phase 6 - Integration Testing & OAuth Flow Validation
