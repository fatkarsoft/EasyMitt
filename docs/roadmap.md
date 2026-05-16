# EasyMitt Roadmap

Last updated: 2026-05-16

## Product North Star

EasyMitt is a Germany-focused SaaS platform for e-invoicing and accounting operations. The intended product shape is:

- simpler and easier than sevDesk for day-to-day invoice work
- stronger than a basic invoicing tool for German compliance
- AI-assisted for import, classification, matching, and review
- focused on Germany only: XRechnung, ZUGFeRD, DATEV, GoBD, Leitweg-ID, German VAT, Mahnwesen

## Completed

### Foundation

- Clean Architecture backend with .NET 10.
- React/Vite frontend with JavaScript-only components.
- Common response envelope.
- Global exception handling.
- Localization in `tr`, `en`, `de`.
- Scalar/OpenAPI docs.
- Role-based auth and authorization.
- PostgreSQL persistence.
- Tenant-scoped SaaS foundation.

### Germany E-Invoicing

- EN16931 BT-code DTO mapping.
- XRechnung export.
- ZUGFeRD PDF export.
- German VAT policy.
- IBAN normalization and validation policy.
- Leitweg-ID fields.

### AI Import

- Local scan service foundation.
- Ollama vision model integration path.
- Scan import API adapter.
- Raw import UI retained.

### SaaS Business Core

- Companies.
- Users.
- Customers with B2B/B2C handling.
- Product/service catalog.
- Inventory movements.
- Invoice draft/customer/product selection flow.

### Sales And Billing

- Invoice list, create, detail.
- Invoice lifecycle.
- Quotes.
- Quote-to-invoice conversion.
- Expenses.
- Bank payments.
- Payment matching/allocation.
- Partial payment status.
- Mahnwesen / dunning.

### Accounting And Compliance Foundation

- Immutable archive foundation.
- DATEV settings.
- DATEV preview.
- DATEV export history.
- Tax key mappings.
- Export locking/force re-export path.

### Compliance Center

- Central compliance dashboard at `/compliance`.
- XRechnung, ZUGFeRD, GoBD, DATEV readiness progress cards.
- Document risk list: missing BT fields, not GoBD archived, not DATEV exported, overdue without reminders.
- Audit timeline per invoice: status transitions, archive, and dunning events.
- Date range, status, and risk level filters.
- All roles read. No new migration.

### Email Delivery

- Send invoice/quote/reminder emails with ZUGFeRD PDF attachment for invoices.
- `email_delivery_logs` table: status (Sent/Failed), toEmail, subject, attachment type, sender.
- SMTP via `System.Net.Mail` with NoOp fallback when SmtpHost is empty.
- `SendEmailModal` component used in InvoiceDetail, QuoteDetail, and Dunning.
- i18n keys for TR/EN/DE.
- Admin/Accountant write, Auditor read-only.

## Next Major Module

### Reporting Dashboard

Goal: Visualize revenue, overdue receivables, VAT summary, and DATEV export coverage.

- Revenue by period.
- Overdue receivables with aging buckets.
- VAT summary by rate.
- DATEV export coverage.
- Top customers by revenue and overdue amount.
- Expense summary by category.

## Upcoming Modules

### Customer Portal

- Customer can view invoices and quotes.
- Download PDF/XML.
- Accept quote.
- See payment status.

### Reporting Dashboard

- Revenue.
- Overdue receivables.
- VAT summary.
- DATEV export coverage.
- Customer/product performance.

### Advanced AI Accounting

- Receipt category suggestions.
- DATEV account suggestions.
- Bank transaction matching confidence.
- Missing e-invoice field suggestions.

### Production Hardening

- Real secret management.
- S3 Object Lock or equivalent immutable archive.
- Schematron validation pipeline.
- Production Peppol Access Point.
- Background jobs.
- Email provider.
- Observability.

## Backlog

- PDF upload support for scan service.
- Multi-page scan analysis.
- Reminder PDF/letter generation.
- Mahngebühren / interest rules if legally and product-wise desired.
- Bank API integration via PSD2 provider, after CSV import is stable.
- DATEV Unternehmen Online style export packaging.
- Admin company settings page.
- User management page.
