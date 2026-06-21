/**
 * NOTE: /send-template is a DEPRECATED convenience route.
 * The recommended approach is to use /send-message directly
 * and render message content in the calling application.
 *
 * These templates are provided as a reference only and should
 * be customized per project.
 */
export function renderTemplate(template, data = {}) {
  switch (template) {
    case 'welcome':
      return `Welcome! Your account #${data.accountId} has been created successfully.`;
    case 'notification':
      return `Notification: ${data.message ?? 'You have a new update.'}`;
    case 'alert':
      return `Alert: ${data.subject ?? 'Important notice'} — ${data.body ?? 'Please check your account.'}`;
    default: {
      const err = new Error('unknown_template');
      err.statusCode = 400;
      throw err;
    }
  }
}
