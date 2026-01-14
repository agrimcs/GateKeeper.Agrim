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
