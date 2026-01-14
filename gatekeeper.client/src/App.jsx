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