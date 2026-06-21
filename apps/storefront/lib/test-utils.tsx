import type { ReactElement, ReactNode } from "react";
import { render } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import en from "@/messages/en.json";

/** Renders a component inside a next-intl provider (English by default for assertions). */
export function renderWithIntl(
  ui: ReactElement,
  { locale = "en", messages = en }: { locale?: string; messages?: Record<string, unknown> } = {},
) {
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <NextIntlClientProvider locale={locale} messages={messages}>
        {children}
      </NextIntlClientProvider>
    );
  }
  return render(ui, { wrapper: Wrapper });
}
