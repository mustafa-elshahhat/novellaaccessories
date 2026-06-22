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
const Coupons = lazy(() => import("@/pages/coupons"));
const CouponDetail = lazy(() => import("@/pages/coupon-detail"));
const TwoOrderSettings = lazy(() => import("@/pages/two-order-settings"));
const Shipping = lazy(() => import("@/pages/shipping"));
const Heroes = lazy(() => import("@/pages/heroes"));
const WhatsAppSettings = lazy(() => import("@/pages/whatsapp-settings"));
const WhatsAppLogs = lazy(() => import("@/pages/whatsapp-logs"));
const Payments = lazy(() => import("@/pages/payments"));
const Expenses = lazy(() => import("@/pages/expenses"));
const Reports = lazy(() => import("@/pages/reports"));
const Analytics = lazy(() => import("@/pages/analytics"));
const Pages = lazy(() => import("@/pages/pages"));
const PageDetail = lazy(() => import("@/pages/page-detail"));
const Seo = lazy(() => import("@/pages/seo"));
const Settings = lazy(() => import("@/pages/settings"));
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
  return <Suspense fallback={<Skeleton lines={8} />}><Routes><Route path="/login" element={auth.status === "authenticated" ? <Navigate to="/dashboard" replace /> : <Login />} /><Route element={<Guard />}><Route index element={<Navigate to="/dashboard" replace />} /><Route path="dashboard" element={<Dashboard />} /><Route path="products" element={<Products />} /><Route path="products/new" element={<ProductDetail mode="new" />} /><Route path="products/:id" element={<ProductDetail />} /><Route path="categories" element={<Categories />} /><Route path="orders" element={<Orders />} /><Route path="orders/:id" element={<OrderDetail />} /><Route path="customers" element={<Customers />} /><Route path="customers/:id" element={<CustomerDetail />} /><Route path="coupons" element={<Coupons />} /><Route path="coupons/new" element={<CouponDetail mode="new" />} /><Route path="coupons/:id" element={<CouponDetail />} /><Route path="coupons/two-order-settings" element={<TwoOrderSettings />} /><Route path="shipping" element={<Shipping />} /><Route path="heroes" element={<Heroes />} /><Route path="whatsapp/settings" element={<WhatsAppSettings />} /><Route path="whatsapp/logs" element={<WhatsAppLogs />} /><Route path="payments" element={<Payments />} /><Route path="expenses" element={<Expenses />} /><Route path="reports" element={<Reports />} /><Route path="analytics" element={<Analytics />} /><Route path="pages" element={<Pages />} /><Route path="pages/:id" element={<PageDetail />} /><Route path="seo" element={<Seo />} /><Route path="settings" element={<Settings />} /></Route><Route path="*" element={<NotFound />} /></Routes></Suspense>;
}

export function App() { return <Providers><AppRoutes /></Providers>; }
