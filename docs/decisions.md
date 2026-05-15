# EasyMitt Decisions

Last updated: 2026-05-16

This file records product and architecture decisions that future agents should preserve unless the user explicitly changes direction.

## Germany Only

Date: 2026-05-15

Decision: EasyMitt targets Germany only.

Reason:

- The product is built around German e-invoicing and accounting operations.
- Core concepts include XRechnung, ZUGFeRD, DATEV, GoBD, Leitweg-ID, German VAT, and Mahnwesen.
- If another country is needed later, it should likely be a separate product/project instead of diluting this codebase.

## Product Positioning

Date: 2026-05-15

Decision: EasyMitt should combine sevDesk-like simplicity with Sovos-like compliance strength.

Reason:

- sevDesk is the ease-of-use benchmark for small business workflows.
- Sovos is the compliance strength reference.
- EasyMitt should be operationally simple while becoming very strong for Germany-specific compliance.

## UI Languages

Date: 2026-05-15

Decision: UI languages remain `tr`, `en`, and `de`.

Reason:

- The market focus is Germany, but the development/user context needs Turkish and English as well.
- This is UI language support, not multi-country product support.

## Frontend Technology

Date: 2026-05-10

Decision: The frontend uses React/Vite with JavaScript files only.

Reason:

- The project originally used TypeScript, then was converted by user request to JavaScript.
- `ui/src` should not contain `.ts` or `.tsx` files.
- Vite has a pre-transform plugin to handle JSX in `.js` files.

## UI Design Direction

Date: 2026-05-10

Decision: Use a Veltrix/admin dashboard style, not a landing page or marketing page.

Reason:

- EasyMitt is an operational SaaS product.
- UI should be dense, calm, professional, responsive, and suitable for repeated daily work.
- List/create/edit patterns should be clear and separate.

## Common Response Envelope

Date: 2026-05-09

Decision: All JSON API endpoints use the standard EasyMitt envelope.

Reason:

- Consistent frontend error handling.
- Traceability with `traceId`.
- Localized messages with `language`.

Exception:

- Successful file download endpoints return files directly.
- File endpoint errors still use the envelope.

## Authorization

Date: 2026-05-09

Decision: Backend policy is authoritative for role-based behavior.

Reason:

- UI may hide/disable write buttons for Auditor, but backend must enforce all write restrictions.

Roles:

- Admin: all operations.
- Accountant: operational write flows.
- Auditor: read-only.

## Domain Policies

Date: 2026-05-10

Decision: Business rules such as German VAT rates, IBAN validation, invoice lifecycle, and retention/compliance policies belong in Domain.

Reason:

- Prevents policy leakage into UI or endpoint code.
- Keeps Clean Architecture boundaries intact.

## Scan Import Provider

Date: 2026-05-12

Decision: Default scan import uses local Ollama via `scan-service`, not direct OpenAI API calls.

Reason:

- User does not want a required extra OpenAI API billing path.
- Local AI can be improved and combined with deterministic parsing.
- OpenAI can be added later as an optional fallback, but should not be default.

## Bank Payments Strategy

Date: 2026-05-15

Decision: Start with CSV import and manual bank transactions before direct bank API integration.

Reason:

- CSV upload is faster and more controllable for MVP.
- German bank API/PSD2 flows require provider selection, consent handling, and production-grade security.
- Direct bank API can come later after matching and allocation workflows are stable.

## Mahnwesen Naming

Date: 2026-05-15

Decision: Use business behavior of archiving/soft handling where appropriate, but label destructive-looking UI actions in user-friendly terms.

Reason:

- The user wants professional business behavior, but frontend labels should match user expectation.
- For customers/products, UI can say "Sil" while backend can preserve safe business semantics if needed.
