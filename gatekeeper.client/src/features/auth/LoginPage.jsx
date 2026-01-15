import { useState, useEffect } from 'react';
import { useNavigate, Link, useSearchParams } from 'react-router-dom';
import { useAuth } from './AuthContext';
import api from '../../services/api';

const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [validationErrors, setValidationErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [searchParams] = useSearchParams();

  const { login } = useAuth();
  const navigate = useNavigate();

  const returnUrl = searchParams.get('returnUrl');

  useEffect(() => {
    console.log('LoginPage mounted');
    console.log('Current URL:', window.location.href);
    console.log('ReturnUrl from searchParams:', returnUrl);
  }, [returnUrl]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setValidationErrors({});
    setLoading(true);

    console.log('=== STARTING LOGIN ===');
    console.log('Login with returnUrl:', returnUrl);
    console.log('returnUrl starts with /connect/authorize?', returnUrl?.startsWith('/connect/authorize'));

    // Check OAuth flow BEFORE login
    const isOAuthFlow = returnUrl && returnUrl.startsWith('/connect/authorize');
    console.log('Is OAuth flow?', isOAuthFlow);

    try {
      console.log('Calling login service...');
      await login(email, password);
      console.log('Login successful!');
      
      // If returnUrl exists and starts with /connect/authorize, establish OAuth session
      if (isOAuthFlow) {
        console.log('Establishing OAuth session for returnUrl:', returnUrl);
        try {
          const response = await api.post('/api/auth/establish-session', { returnUrl });
          console.log('Session established, redirecting to:', response.data.returnUrl);
          console.log('response.data:', response.data);
          console.log('About to redirect with window.location.href =', response.data.returnUrl);
          
          // Use full URL to avoid React Router interception
          const fullUrl = `https://localhost:44330${response.data.returnUrl}`;
          console.log('Full redirect URL:', fullUrl);
          window.location.href = fullUrl;
          return;
        } catch (err) {
          console.error('Session establishment error:', err);
          const errorDetail = err.response?.data?.detail || err.response?.data?.message || err.message;
          setError(`Failed to establish OAuth session: ${errorDetail}`);
          setLoading(false);
          return;
        }
      }
      
      console.log('No OAuth flow, navigating to admin');
      // Normal login - go to admin
      navigate('/admin/clients');
    } catch (err) {
      if (err.errors && Object.keys(err.errors).length > 0) {
        setValidationErrors(err.errors);
        setError('Please fix the validation errors below.');
      } else {
        setError(err.message || 'Login failed. Please check your credentials.');
      }
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
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                validationErrors.Email ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
              }`}
              placeholder="john.doe@example.com"
            />
            {validationErrors.Email && (
              <p className="text-red-600 text-sm mt-1">{validationErrors.Email.join(', ')}</p>
            )}
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
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                validationErrors.Password ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
              }`}
              placeholder="••••••••"
            />
            {validationErrors.Password && (
              <p className="text-red-600 text-sm mt-1">{validationErrors.Password.join(', ')}</p>
            )}
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
