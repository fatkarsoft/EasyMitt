import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "./theme.css";
import { AuthProvider, useAuth } from "./state/auth.js";
import Layout from "./components/Layout.js";
import Login from "./pages/Login.js";
import Dashboard from "./pages/Dashboard.js";
import Drafts from "./pages/Drafts.js";
import InvoiceForm from "./pages/InvoiceForm.js";
import RawImport from "./pages/RawImport.js";
import InvoiceDetail from "./pages/InvoiceDetail.js";
import Settings from "./pages/Settings.js";

function ProtectedApp() {
  const { session } = useAuth();
  if (!session) return <Navigate to="/login" replace />;

  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/invoices" element={<Drafts />} />
        <Route path="/invoices/new" element={<InvoiceForm />} />
        <Route path="/invoices/raw" element={<RawImport />} />
        <Route path="/invoices/:id" element={<InvoiceDetail />} />
        <Route path="/settings" element={<Settings />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Layout>
  );
}

function App() {
  const { session } = useAuth();
  return (
    <Routes>
      <Route path="/login" element={session ? <Navigate to="/dashboard" replace /> : <Login />} />
      <Route path="/*" element={<ProtectedApp />} />
    </Routes>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
