import ReactDOM from "react-dom/client";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "./theme.css";
import { AuthProvider, useAuth } from "./state/auth.js";
import Layout from "./components/Layout.js";
import Login from "./pages/Login.js";
import Dashboard from "./pages/Dashboard.js";
import Customers from "./pages/Customers.js";
import CustomerForm from "./pages/CustomerForm.js";
import Quotes from "./pages/Quotes.js";
import QuoteForm from "./pages/QuoteForm.js";
import QuoteDetail from "./pages/QuoteDetail.js";
import Expenses from "./pages/Expenses.js";
import ExpenseForm from "./pages/ExpenseForm.js";
import Payments from "./pages/Payments.js";
import Dunning from "./pages/Dunning.js";
import Drafts from "./pages/Drafts.js";
import InvoiceForm from "./pages/InvoiceForm.js";
import Products from "./pages/Products.js";
import ProductForm from "./pages/ProductForm.js";
import InventoryMovements from "./pages/InventoryMovements.js";
import InventoryMovementForm from "./pages/InventoryMovementForm.js";
import RawImport from "./pages/RawImport.js";
import InvoiceDetail from "./pages/InvoiceDetail.js";
import Datev from "./pages/Datev.js";
import DatevPreview from "./pages/DatevPreview.js";
import Compliance from "./pages/Compliance.js";
import Reporting from "./pages/Reporting.js";
import Settings from "./pages/Settings.js";

function ProtectedApp() {
  const { session } = useAuth();
  if (!session) return <Navigate to="/login" replace />;

  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/customers" element={<Customers />} />
        <Route path="/customers/new" element={<CustomerForm />} />
        <Route path="/customers/:id/edit" element={<CustomerForm />} />
        <Route path="/quotes" element={<Quotes />} />
        <Route path="/quotes/new" element={<QuoteForm />} />
        <Route path="/quotes/:id/edit" element={<QuoteForm />} />
        <Route path="/quotes/:id" element={<QuoteDetail />} />
        <Route path="/expenses" element={<Expenses />} />
        <Route path="/expenses/new" element={<ExpenseForm />} />
        <Route path="/expenses/:id/edit" element={<ExpenseForm />} />
        <Route path="/payments" element={<Payments />} />
        <Route path="/dunning" element={<Dunning />} />
        <Route path="/invoices" element={<Drafts />} />
        <Route path="/invoices/new" element={<InvoiceForm />} />
        <Route path="/invoices/raw" element={<RawImport />} />
        <Route path="/products" element={<Products />} />
        <Route path="/products/new" element={<ProductForm />} />
        <Route path="/products/:id/edit" element={<ProductForm />} />
        <Route path="/inventory" element={<InventoryMovements />} />
        <Route path="/inventory/new" element={<InventoryMovementForm />} />
        <Route path="/invoices/:id" element={<InvoiceDetail />} />
        <Route path="/datev" element={<Datev />} />
        <Route path="/datev/preview" element={<DatevPreview />} />
        <Route path="/compliance" element={<Compliance />} />
        <Route path="/reporting" element={<Reporting />} />
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
  <BrowserRouter>
    <AuthProvider>
      <App />
    </AuthProvider>
  </BrowserRouter>
);
