/** Whether `href` (locale-stripped) is the active route for `pathname` (locale-stripped). */
export function isActivePath(pathname: string, href: string): boolean {
  if (href === "/") return pathname === "/";
  return pathname === href || pathname.startsWith(`${href}/`);
}
