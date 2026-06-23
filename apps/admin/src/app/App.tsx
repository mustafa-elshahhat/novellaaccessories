import { lazy, Suspense } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { Providers } from "./providers";
import { AdminShell } from "./shell";
import { useAuth } from "./auth-context";
import { ErrorState, Skeleton } from "@/components/ui";

const Login = lazy(() => import("@/pages/login"));
const Dashboard = lazy(() => import("@/pages/dashboard"));
const Products = lazy(() => import("@/pages/products"));
const ProductDetail = lazy(() => import("@/pages/product-detail"));
const Categories = lazy(() => import("@/pages/categories"));
const Orders = lazy(() => import("@/pages/orders"));
const OrderDetail = lazy(() => import("@/pages/order-detail"));
const Customers = lazy(() => import("@/pages/customers"));
const CustomerDetail = lazy(() => import("@/pages/customer-detail"));
const Discounts = lazy(() => import("@/pages/discounts"));
const CouponDetail = lazy(() => import("@/pages/coupon-detail"));
const Shipping = lazy(() => import("@/pages/shipping"));
const Content = lazy(() => import("@/pages/content"));
const PageDetail = lazy(() => import("@/pages/page-detail"));
const WhatsApp = lazy(() => import("@/pages/whatsapp"));
const Expenses = lazy(() => import("@/pages/expenses"));
const Reports = lazy(() => import("@/pages/reports"));
const NotFound = lazy(() => import("@/pages/not-found"));

function Guard() {
  const auth = useAuth();
  const location = useLocation();
  if (auth.status === "loading") return <Skeleton lines={8} />;
  if (auth.status === "forbidden") return <ErrorState error={new Error("Access denied. Admin access is required.")} />;
  if (auth.status !== "authenticated") return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return <AdminShell />;
}

function AppRoutes() {
  const auth = useAuth();
  return <Suspense fallback={<Skeleton lines={8} />}><Routes><Route path="/login" element={auth.status === "authenticated" ? <Navigate to="/dashboard" replace /> : <Login />} /><Route element={<Guard />}><Route index element={<Navigate to="/dashboard" replace />} /><Route path="dashboard" element={<Dashboard />} /><Route path="products" element={<Products />} /><Route path="products/new" element={<ProductDetail mode="new" />} /><Route path="products/:id" element={<ProductDetail />} /><Route path="categories" element={<Categories />} /><Route path="orders" element={<Orders />} /><Route path="orders/:id" element={<OrderDetail />} /><Route path="customers" element={<Customers />} /><Route path="customers/:id" element={<CustomerDetail />} /><Route path="discounts" element={<Discounts />} /><Route path="discounts/new" element={<CouponDetail mode="new" />} /><Route path="discounts/:id" element={<CouponDetail />} /><Route path="shipping" element={<Shipping />} /><Route path="content" element={<Content />} /><Route path="content/pages/:id" element={<PageDetail />} /><Route path="whatsapp" element={<WhatsApp />} /><Route path="expenses" element={<Expenses />} /><Route path="reports" element={<Reports />} /></Route><Route path="*" element={<NotFound />} /></Routes></Suspense>;
}

export function App() { return <Providers><AppRoutes /></Providers>; }
