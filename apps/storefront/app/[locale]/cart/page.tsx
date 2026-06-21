import type { Metadata } from "next";
import { setRequestLocale } from "next-intl/server";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { CartView } from "@/features/cart/cart-view";

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "cart", "title");
}

export default async function CartPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  return <CartView />;
}
