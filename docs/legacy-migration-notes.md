# Legacy site migration notes

Extracted from `superioreconomymoving-com.zip` (cPanel backup of `superioreconomymoving.com`, dated 2022-08-16). This document records what business logic existed on the legacy PHP site, what data lives off-site, what was reproduced in the new static site, and what still needs follow-up before launch.

> **Brand rename (2026-05-24):** The business is rebranded from **Superior Economy Moving** to **Superior Moving Co.** (formal name) / **Superior Moving** (common name). All new pages use the new name. The legacy backup, the existing `superioreconomymoving.com` domain, and identifiers like the BCA report URL (`/superior-moving-13061994`), the Instagram handle `@superioreconomymoving`, and the email `jack@economymoving.net` still carry the old branding — those are operational identifiers and stay as-is until each is migrated. The canonical URL placeholder in `<link rel="canonical">` is currently `https://superioreconomymoving.com/`; **decide the new primary domain before launch** and update the canonicals in one pass.

## 1. Legacy lead-capture forms (PHP `mail()`)

Two simple PHP scripts collected leads and emailed them directly. No database write, no CRM, no logs beyond the mail server. All leads went to `economy1003@gmail.com`.

| Legacy file | Fields collected | Destination | Notes |
|---|---|---|---|
| `estimate.php` | name, phone, email | `mail("economy1003@gmail.com", ...)` | Plain form-mail. Redirects to `http://www.economymoving.net/thanks.html` on success. Uses deprecated `eregi` regex (PHP 5.x). |
| `contact_form.php` | name, email, phone, referral, details | `mail("economy1003@gmail.com", ...)` | Uses Securimage CAPTCHA. Redirects to `mover_references.html` (the references page) on success. |

**Migrated as:** static HTML forms at `/contact.html` and `/quote.html` with `mailto:economy1003@gmail.com` as a working-now fallback. Both pages include a clear TODO comment to wire up a real form endpoint (Formspree, Netlify Forms, or Azure Static Web Apps API) and add a CAPTCHA equivalent (Turnstile/reCAPTCHA) before any push for traffic.

## 2. InstaQuote — full inventory + price calculator

The InstaQuote tool (`get_an_online_moving_quote.php` + `instaquoteapp.php` + `instaquote.js` + `insert.php`) is the most substantial legacy logic. It collects a complete furniture inventory, computes a firm estimate in the browser via JavaScript, and writes a row to MySQL.

### 2a. Data storage

- **Database:** MySQL, `economy_instaquote.quotes` table
- **Connection file:** `Connections/writeQuote.php` (also `getQuote.php` — identical creds, used by the admin viewer)
- **Schema:** ~170 columns — one column per furniture item across 11 room/category groups (`LR_`, `DR_`, `KIT_`, `BR_`, `OFC_`, `GAR_`, `APL_`, `BULKY_`, `PAT_`, `MISC_`, `BOX_`), plus contact, addresses, miles, and computed totals (`subtotal_*`, `totalPcs`, `totalCuFt`, `totalMen`, `totalHours`, `totalDriveTime`, `totalHrRate`, `totalFuelCharge`, `totalPrice`, `totalWeight`).
- **Admin viewer:** `admin/index.php`, `viewquote.php`, `quote_view.php`, `quote_update.php`, `quote_delete.php` — Dreamweaver / MX Kollection session auth.

### 2b. Quote calculation logic (from `instaquote.js`)

All math runs client-side. Inputs are furniture counts + one-way mileage. The PHP layer only persists the resulting row — it does **not** recompute.

**Cubic feet per item** (representative samples — full table in `instaquote.js` lines 355–525):

| Item | cu ft | Item | cu ft |
|---|---|---|---|
| Sofa | 50 | King/Queen bed | 70 |
| Loveseat | 35 | Double bed | 60 |
| Sectional piece | 30 | Single bed | 40 |
| Recliner / oversized chair | 25 | Bunk bed | 70 |
| Arm chair | 12 | Dresser (double) | 50 |
| Occasional chair | 15 | Chest of drawers | 25 |
| Coffee/sofa table | 12 | Treadmill | 10 |
| End / small table | 5 | Baby grand piano | 70 |
| Tube TV | 10 | Spinet piano | 60 |
| Flat-panel TV | 7 | Upright piano | 70 |
| Bookcase (large) | 20 | Big-screen TV | 40 |
| Bookcase (small) | 5 | Pool table | 40 |
| Large desk | 35 | Top/bottom fridge | 45 |
| Small desk | 22 | Side-by-side fridge | 65 |
| Entertainment center | 20 | Washer / dryer | 25 each |
| Dining table | 30 | Stove | 30 |
| Buffet (bottom) | 30 | Small box | 1.5 |
| Buffet (top) | 20 | Medium box | 3 |
| Conference table | 40 | Large box | 4.5 |
| Lamp | 3 | Wardrobe box | 10 |

**Drive time** (California "double drive time"):
```
miles = max(miles, 10)
driveTime = round((miles / 45) * 2 * 4) / 4   // 45 mph, doubled for return, rounded to ¼ hr
```

**Fuel surcharge:**
```
fuelCharge = max(15, (miles * 2 / 5) * 2.50)   // 1 gal per 5 mi, $2.50/gal, round-trip
```

**Man-hours of labor:**
```
manHrs = (pieces / 3) + ((boxes * 0.075) / 3) + ((wardrobeBoxes * 0.3) / 3)
```

**Crew size (men):**
```
menQty = ceil(manHrs / 9)
menQty = max(menQty, 2)                       // 2-man minimum
if any bulky item present: menQty = max(menQty, 3)   // 3-man minimum for bulky
```

**Hourly rates (as priced in the 2022 snapshot — assume outdated):**

| Crew | Hourly rate |
|---|---|
| 2 men | $104.00 |
| 3 men | $145.00 |
| 4 men | $186.00 |
| 5 men | $221.00 |
| 6 men | $257.00 |
| 7 men | $295.00 |
| 8 men | $333.00 |
| 9 men | $371.00 |
| 10 men | $409.00 |

**Total move time and price:**
```
moveHours = manHrs / menQty + driveTime
moveHours = max(moveHours, 2)                 // 2-hour minimum
moveMinutes = round to nearest 15 minutes
price = moveHours * hourlyRate + fuelCharge
```

**Estimated weight:** `cubicFeet * 7` (lbs)

### 2c. What the new static site does instead

The static rebuild **does not replicate the calculator**. Three reasons:

1. The rate table is from 2022 and almost certainly out of date — shipping wrong prices to leads is worse than no number at all.
2. The calculator requires a live database to persist the long-form inventory, which a static host can't provide.
3. The deep-research report's Phase-3 recommendation is to rebuild quote/intake on a modern stack (custom intake + CRM + Google Routes API + payment deposit), not to mirror the legacy SQL schema.

Instead:

- `/resources/pricing-factors.html` documents the **methodology** in customer-friendly language — "double drive time" explanation, what drives crew size, what affects total hours, what fuel surcharge covers — without quoting stale dollar figures.
- `/quote.html` is a short lead form (contact + move size + dates + mileage estimate + box counts + bulky items) that emails the rep. The legacy long-form inventory grid is preserved as an idea (see "Carton guide" PDF link) but is not the entry point.
- The full legacy calculator (`instaquote.js`) is preserved in this repo for reference under `.legacy-extract/` until it can be rebuilt — but `.legacy-extract/` should be deleted after rates are reconfirmed and a new estimator is scoped.

## 3. Credentials exposure — action required

The legacy backup shipped a plaintext MySQL credential in `Connections/writeQuote.php`:

```
host:     localhost
database: economy_instaquote
user:     economy_iqwrite
password: EM.Db-8418
```

These were in a public_html backup. **Assume compromised.** Before pointing DNS at any host that still has this database reachable, rotate the password (or, better, retire the user and create a fresh credential under a new name when the new quote pipeline is built). The `superioreconomymoving.com` admin pages also use Dreamweaver session-based auth (`MM_Username`) with no rate-limiting and no MFA — do not re-enable the legacy admin on a live domain.

## 4. Resources from legacy that were re-hosted

These were preserved and now ship from the new site root (no longer dependent on `superioreconomymoving.com`):

| Asset | Old location | New location | Notes |
|---|---|---|---|
| Moving Guide | `/pdf/Superior_Economy_Moving_Guide.pdf` | `/pdf/Superior_Economy_Moving_Guide.pdf` | Copied as-is. |
| Carton guide | `/pdf/carton_guide.pdf` | `/pdf/carton_guide.pdf` | Copied as-is. |
| Cube sheet | `/pdf/cube_sheet.pdf` | `/pdf/cube_sheet.pdf` | Copied as-is. |
| Office moving checklist | `/pdf/Superior_Economy_Office_Moving_Check_List.pdf` | `/pdf/Superior_Economy_Office_Moving_Check_List.pdf` | Copied as-is. |
| Packing guide (HTML) | `/diy-packing.html` | `/resources/packing-guide.html` | Reformatted, content preserved, modernized to current UI. |
| Disposal & donations | `/disposal_and_donations.html` | `/resources/disposal-and-donations.html` | Reformatted; phone numbers and addresses kept as written — verify before launch (many are 2011-era). |
| Testimonials | `/mover_references.html` | `/reviews.html` | Six legacy testimonials preserved; add a CTA to drive new Google reviews. |
| Privacy policy | `/we_respect_your_privacy.html` | `/privacy.html` | Rewritten as a first-party canonical policy (the old `privacy.html` was a "mirror" pointer — that's been replaced). |
| Services list | `/moving_services.html` | Folded into `/services/*` pages | Bullet copy salvaged into each service page. |
| About | `/about_superior_economy_moving.html` | `/about.html` | Family-business narrative preserved; Yellow Pages Flash video reference removed. |
| Contact info | `/contact_los_angeles_mover.html` | `/contact.html` | Form fields preserved (without the deprecated CAPTCHA). |

Legacy pages **not** ported (low value or broken):

- `garage_sale.html` — most outbound links are dead (Recycler, PennySaver). If wanted, can be folded into `/resources/disposal-and-donations.html`.
- `econolist.html`, `helppack.html`, `helppack2.html`, `pg2_sub.html`, `home.html`, `work.html`, `nortonsw_*.html`, `thanks.html`, `sitemap.html` — duplicates, stubs, or template scaffolding from the original Dreamweaver build.

## 5. Contact data points to keep consistent (the canonical NAP)

| Field | Value | Source |
|---|---|---|
| Business name (formal) | Superior Moving Co. | User confirmation 2026-05-24 (rename from "Superior Economy Moving LLC" per Yellow Pages embed in legacy `about_superior_economy_moving.html`) |
| Business name (common) | Superior Moving | User confirmation 2026-05-24 |
| Primary phone | (818) 884-6125 | Both legacy sites, prototype |
| Toll-free phone | (800) 324-0334 | `contact_los_angeles_mover.html` |
| Owner email | jack@economymoving.net | `contact_los_angeles_mover.html` (obfuscated in JS) |
| Lead-routing email | economy1003@gmail.com | `estimate.php`, `contact_form.php` |
| BCA report | https://www.checkbca.org/report/superior-moving-13061994 | Linked from prototype |
| Instagram | https://www.instagram.com/superioreconomymoving | Linked from prototype |
| Owners (per legacy About) | Jack, Regina & Robb Wolford | `about_superior_economy_moving.html` |
| Crew tenure | 15 years average | Same |
| Years in business | 50+ | Same |

The deep-research report (section "Highest priority" in the Applied Playbook) flags that exposing multiple phone numbers on a page creates local-SEO ambiguity. The new site uses **only (818) 884-6125** in headers/footers/CTAs; the toll-free is kept only on `/contact.html` and `/about.html` for callers who recognize it.

## 6. What's still TODO before launch

These are out of scope for the static rebuild but should be done before the new domain replaces the legacy one in search results:

1. **Rotate the MySQL credential** above (or retire the user).
2. **Take down `economymoving.net`** (the Thryv-hosted overlap) once DNS for the chosen primary domain is live; the user controls that domain. Don't kill `superioreconomymoving.com` yet — it still hosts PDFs that the legacy Google index references; 301-redirect each legacy URL to its new equivalent (see redirect map below).
3. **Reconfirm hourly rates** with Jack/Regina/Robb before publishing any pricing. The numbers in §2b are 2022 snapshots.
4. **Wire up the lead forms** — replace the `mailto:` fallback in `/contact.html` and `/quote.html` with a real endpoint and CAPTCHA. Add server-side validation and email notifications to both `economy1003@gmail.com` and a backup address.
5. **Verify the 2011-era phone numbers** on `/resources/disposal-and-donations.html` are still valid (LA Sanitation, Calabasas Landfill, Goodwill, Salvation Army, Council Thrift Stores).
6. **Claim/verify Google Business Profile, Apple Business, Bing Places** under the canonical NAP above (research report §"Open questions").
7. **Add a real form endpoint, analytics (GA4 + Search Console), Consent Mode** when ready to point search traffic at the new site.

### Suggested 301 redirect map (when retiring legacy URLs)

| Legacy URL | New URL |
|---|---|
| `/index.html`, `/home.html` | `/` |
| `/about.html`, `/about_us.html`, `/about_superior_economy_moving.html` | `/about.html` |
| `/contact.html`, `/contact_us.html`, `/contact_los_angeles_mover.html` | `/contact.html` |
| `/moving_services.html`, `/services.html`, `/our_services.html` | `/services/` (hub — to be added if needed, else 301 to `/services/local-moving-los-angeles.html`) |
| `/moving_information.html`, `/help.html` | `/resources/` |
| `/diy-packing.html`, `/helppack.html`, `/helppack2.html` | `/resources/packing-guide.html` |
| `/disposal_and_donations.html`, `/garage_sale.html` | `/resources/disposal-and-donations.html` |
| `/mover_references.html`, `/references.html` | `/reviews.html` |
| `/we_respect_your_privacy.html`, `/privacy.html` | `/privacy.html` |
| `/get_an_online_moving_quote.php`, `/estimate.php`, `/insert.php`, `/instaquoteapp.php`, `/viewquote.php` | `/quote.html` |
| `/contact_form.php` | `/contact.html` |
| `/admin/*`, `/login*.php` | 410 Gone (or block at the edge) |
| `/Cube_Sheet.html`, `/Cube Sheet.html` | `/pdf/cube_sheet.pdf` |
