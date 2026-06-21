import type { ButtonHTMLAttributes, CSSProperties, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { useId, useState } from "react";
import { Link } from "react-router-dom";
import { formatDate, safeText } from "@/lib/format";

export function Button({ className = "", variant = "primary", ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "danger" | "ghost" }) {
  return <button className={`button button-${variant} ${className}`} {...props} />;
}

export function LinkButton({ to, children, variant = "secondary" }: { to: string; children: ReactNode; variant?: "primary" | "secondary" | "ghost" }) {
  return <Link className={`button button-${variant}`} to={to}>{children}</Link>;
}

export function IconButton(props: ButtonHTMLAttributes<HTMLButtonElement> & { label: string }) {
  const { label, children, ...rest } = props;
  return <button className="icon-button" aria-label={label} title={label} {...rest}>{children}</button>;
}

export function Field({ label, error, children, hint }: { label: string; error?: string; hint?: string; children: ReactNode }) {
  const id = useId();
  return <label className="field" htmlFor={id}><span>{label}</span>{children}{hint ? <small>{hint}</small> : null}{error ? <strong className="field-error">{error}</strong> : null}</label>;
}

export function Input(props: InputHTMLAttributes<HTMLInputElement>) { return <input className="input" {...props} />; }
export function PasswordInput(props: InputHTMLAttributes<HTMLInputElement>) { return <input className="input" type="password" autoComplete="current-password" {...props} />; }
export function NumberInput(props: InputHTMLAttributes<HTMLInputElement>) { return <input className="input" type="number" step="1" {...props} />; }
export function CurrencyInput(props: InputHTMLAttributes<HTMLInputElement>) { return <input className="input" type="number" step="0.01" min="0" inputMode="decimal" {...props} />; }
export function PercentageInput(props: InputHTMLAttributes<HTMLInputElement>) { return <input className="input" type="number" step="0.01" min="0" max="100" inputMode="decimal" {...props} />; }
export function DateInput(props: InputHTMLAttributes<HTMLInputElement>) { return <input className="input" type="datetime-local" {...props} />; }
export function Select(props: SelectHTMLAttributes<HTMLSelectElement>) { return <select className="input" {...props} />; }
export function Textarea(props: TextareaHTMLAttributes<HTMLTextAreaElement>) { return <textarea className="input textarea" {...props} />; }

export function Card({ children, className = "", style }: { children: ReactNode; className?: string; style?: CSSProperties }) { return <section className={`card ${className}`} style={style}>{children}</section>; }
export function KpiCard({ label, value, to, tone }: { label: string; value: ReactNode; to?: string; tone?: string }) { const content = <Card className={`kpi ${tone ?? ""}`}><span>{label}</span><strong>{value}</strong></Card>; return to ? <Link className="unstyled" to={to}>{content}</Link> : content; }

export function PageHeader({ title, actions, eyebrow }: { title: string; actions?: ReactNode; eyebrow?: string }) {
  return <header className="page-header"><div>{eyebrow ? <p className="eyebrow">{eyebrow}</p> : null}<h1>{title}</h1></div><div className="page-actions">{actions}</div></header>;
}

export function StatusBadge({ value }: { value: string | boolean | number | null | undefined }) {
  const text = String(value ?? "Unknown");
  const tone = /active|paid|sent|delivered|connected|true|configured|success/i.test(text) ? "success" : /pending|preparing|shipped|warning/i.test(text) ? "warning" : /fail|cancel|inactive|false|not/i.test(text) ? "danger" : "neutral";
  return <span className={`badge badge-${tone}`}>{text}</span>;
}

export function EmptyState({ title = "No data yet", body = "When records are available they will appear here." }) { return <div className="empty"><strong>{title}</strong><p>{body}</p></div>; }
export function ErrorState({ error }: { error: unknown }) { return <div className="error-state" role="alert"><strong>Request failed</strong><p>{error instanceof Error ? error.message : "The request could not be completed."}</p></div>; }
export function Skeleton({ lines = 4 }: { lines?: number }) { return <div className="skeleton-wrap">{Array.from({ length: lines }).map((_, i) => <span className="skeleton" key={i} />)}</div>; }

export function Toast({ message, onClose }: { message?: string | null; onClose: () => void }) {
  if (!message) return null;
  return <div className="toast" role="status"><span>{message}</span><button onClick={onClose} aria-label="Dismiss message">×</button></div>;
}

export function ConfirmDialog({ title, body, confirmLabel = "Confirm", destructive, onConfirm, children }: { title: string; body: string; confirmLabel?: string; destructive?: boolean; onConfirm: () => void; children: (open: () => void) => ReactNode }) {
  const [open, setOpen] = useState(false);
  return <>{children(() => setOpen(true))}{open ? <div className="dialog-backdrop" role="presentation"><div className="dialog" role="dialog" aria-modal="true" aria-labelledby="confirm-title"><h2 id="confirm-title">{title}</h2><p>{body}</p><div className="dialog-actions"><Button variant="secondary" onClick={() => setOpen(false)}>Cancel</Button><Button variant={destructive ? "danger" : "primary"} onClick={() => { setOpen(false); onConfirm(); }}>{confirmLabel}</Button></div></div></div> : null}</>;
}

export type Column<T> = { key: string; header: string; render?: (row: T) => ReactNode; numeric?: boolean };
export function DataTable<T extends Record<string, any>>({ caption, rows, columns, empty, loading }: { caption: string; rows?: T[]; columns: Column<T>[]; empty?: string; loading?: boolean }) {
  if (loading) return <Skeleton lines={6} />;
  if (!rows?.length) return <EmptyState title={empty ?? "No records"} />;
  return <div className="table-wrap"><table><caption>{caption}</caption><thead><tr>{columns.map((column) => <th className={column.numeric ? "numeric" : undefined} key={column.key}>{column.header}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={String(row.id ?? index)}>{columns.map((column) => <td className={column.numeric ? "numeric" : undefined} key={column.key}>{column.render ? column.render(row) : renderValue(row[column.key])}</td>)}</tr>)}</tbody></table></div>;
}

function renderValue(value: unknown) {
  if (typeof value === "boolean") return <StatusBadge value={value} />;
  if (typeof value === "string" && /At$|Date$/i.test(value)) return formatDate(value);
  return safeText(value);
}

export function Pagination({ page, totalPages, onPage }: { page: number; totalPages: number; onPage: (page: number) => void }) {
  return <nav className="pagination" aria-label="Pagination"><Button variant="secondary" disabled={page <= 1} onClick={() => onPage(page - 1)}>Previous</Button><span>Page {page} of {Math.max(totalPages, 1)}</span><Button variant="secondary" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>Next</Button></nav>;
}

export function SecretStatus({ configured, label = "Secret" }: { configured: boolean; label?: string }) { return <span className="secret-status"><span>{label}</span><StatusBadge value={configured ? "configured" : "not configured"} /></span>; }
export function CopyButton({ value }: { value?: string | null }) { return <Button variant="ghost" disabled={!value} onClick={() => value && navigator.clipboard.writeText(value)}>Copy</Button>; }
export function JsonViewer({ data }: { data: unknown }) { return <pre className="json-viewer">{JSON.stringify(data, null, 2)}</pre>; }
export function Tabs({ tabs, active, onChange }: { tabs: string[]; active: string; onChange: (tab: string) => void }) { return <div className="tabs" role="tablist">{tabs.map((tab) => <button role="tab" aria-selected={tab === active} className={tab === active ? "active" : ""} key={tab} onClick={() => onChange(tab)}>{tab}</button>)}</div>; }

export function ChartBars({ rows }: { rows: { label: string; value: number }[] }) {
  const max = Math.max(...rows.map((r) => r.value), 1);
  return <div className="chart" role="img" aria-label="Bar chart with table alternative">{rows.map((row) => <div className="bar-row" key={row.label}><span>{row.label}</span><i style={{ width: `${(row.value / max) * 100}%` }} /><b>{row.value}</b></div>)}</div>;
}
