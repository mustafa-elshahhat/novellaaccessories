import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "./auth-context";
import { Button, Toast } from "@/components/ui";
import { env } from "@/lib/env";

export const navigation = [
  { group: "Overview", items: [{ label: "Dashboard", to: "/dashboard" }] },
  { group: "Catalog", items: [{ label: "Products", to: "/products" }, { label: "Categories", to: "/categories" }] },
  { group: "Orders & Customers", items: [{ label: "Orders", to: "/orders" }, { label: "Customers", to: "/customers" }] },
  { group: "Marketing", items: [{ label: "Coupons", to: "/coupons" }, { label: "Two-order settings", to: "/coupons/two-order-settings" }, { label: "Heroes", to: "/heroes" }] },
  { group: "Operations", items: [{ label: "Shipping", to: "/shipping" }, { label: "WhatsApp settings", to: "/whatsapp/settings" }, { label: "WhatsApp logs", to: "/whatsapp/logs" }, { label: "Payments", to: "/payments" }, { label: "Expenses", to: "/expenses" }] },
  { group: "Content", items: [{ label: "Pages", to: "/pages" }, { label: "SEO", to: "/seo" }] },
  { group: "Reports", items: [{ label: "Reports", to: "/reports" }, { label: "Analytics", to: "/analytics" }] },
  { group: "System", items: [{ label: "Settings", to: "/settings" }] }
];

export function AdminShell() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const crumbs = location.pathname.split("/").filter(Boolean);
  return <div className="admin-shell"><aside className="sidebar"><div className="brand"><span>N</span><strong>{env.appName}</strong></div><nav aria-label="Admin navigation">{navigation.map((section) => <section key={section.group}><h2>{section.group}</h2>{section.items.map((item) => <NavLink key={item.to} to={item.to}>{item.label}</NavLink>)}</section>)}</nav></aside><div className="shell-main"><header className="topbar"><button className="drawer-toggle" aria-label="Open navigation">Menu</button><nav className="breadcrumb" aria-label="Breadcrumb"><a onClick={() => navigate("/dashboard")}>Admin</a>{crumbs.map((crumb) => <span key={crumb}>{crumb}</span>)}</nav><div className="identity"><span>{auth.admin?.displayName}</span><Button variant="secondary" onClick={() => void auth.logout()}>Logout</Button></div></header><main id="main-content"><Outlet /></main></div><Toast message={auth.message} onClose={auth.clearMessage} /></div>;
}
