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

## Next Major Module

### Compliance Center

Goal: Make EasyMitt feel like a serious German compliance product instead of only an invoice entry tool.

Recommended complete scope:

- Central compliance dashboard.
- Readiness cards:
  - XRechnung readiness
  - ZUGFeRD readiness
  - DATEV readiness
  - GoBD archive status
  - payment reconciliation status
  - Mahnwesen overdue risk
- Document risk list:
  - missing required BT fields
  - invalid VAT/IBAN
  - not archived
  - not exported to DATEV
  - overdue/unpaid
- Audit timeline:
  - draft created
  - validated
  - exported
  - archived
  - sent/issued/paid/overdue
  - reminder created
  - DATEV exported
- Filters:
  - date range
  - status
  - document type
  - risk level
- Role behavior:
  - Auditor read-only
  - Admin/Accountant can trigger validation/export actions

## Upcoming Modules

### Email Delivery

- Send invoice/quote/reminder emails.
- Store delivery status.
- Template management.
- Attach XRechnung/ZUGFeRD outputs.

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
