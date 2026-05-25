// InstaQuote — live moving estimate.
// Fetches the item catalog from the API, renders category sections, debounces input changes,
// and POSTs the request to /api/quote/calculate for the live price; the page form itself
// submits to /quote/submit which renders the receipt page server-side.
(function () {
  "use strict";

  var form = document.getElementById("instaquote-form");
  var categoriesContainer = document.getElementById("iq-categories");
  if (!form || !categoriesContainer) return;

  var quoteCard = form.querySelector("[data-iq-quote]");
  var priceEl = form.querySelector("[data-iq-price]");
  var statusEl = form.querySelector("[data-iq-status]");
  var statsEl = form.querySelector("[data-iq-stats]");
  var fields = {
    pieces: form.querySelector("[data-iq-pieces]"),
    cuft: form.querySelector("[data-iq-cuft]"),
    crew: form.querySelector("[data-iq-crew]"),
    hours: form.querySelector("[data-iq-hours]"),
    drive: form.querySelector("[data-iq-drive]"),
    fuel: form.querySelector("[data-iq-fuel]")
  };

  // Catalog data is inlined here so the page renders without a round-trip on first paint.
  // Order and keys must stay in sync with api/Quote/ItemCatalog.cs.
  var CATEGORIES = [
    { key: "lr", name: "Living Room" },
    { key: "dr", name: "Dining Room" },
    { key: "kit", name: "Kitchen" },
    { key: "br", name: "Bedroom" },
    { key: "ofc", name: "Office" },
    { key: "grg", name: "Garage" },
    { key: "apl", name: "Appliances" },
    { key: "blky", name: "Bulky / Specialty" },
    { key: "pto", name: "Patio" },
    { key: "misc", name: "Miscellaneous" }
  ];
  var ITEMS = [
    ["lr_bench","lr","Bench"],["lr_bookcase_lg","lr","Bookcase (large)"],["lr_bookcase_sm","lr","Bookcase (small)"],
    ["lr_music_cabinet","lr","Music cabinet"],["lr_chair_arm","lr","Arm chair"],["lr_chair_occ","lr","Occasional chair"],
    ["lr_chair_over","lr","Oversized chair"],["lr_clock_grand","lr","Grandfather clock"],["lr_desk_sect","lr","Secretary desk"],
    ["lr_desk_sm","lr","Small desk"],["lr_ent_center","lr","Entertainment center"],["lr_footstool","lr","Footstool"],
    ["lr_lamp","lr","Lamp"],["lr_mirror","lr","Mirror"],["lr_picture_sm","lr","Picture (small)"],
    ["lr_picture_lg","lr","Picture (large)"],["lr_plant_stand","lr","Plant stand"],["lr_sofa_sect","lr","Sectional sofa (per piece)"],
    ["lr_loveseat","lr","Loveseat"],["lr_sofa","lr","Sofa"],["lr_table_sm","lr","Small table"],
    ["lr_table_sofa","lr","Sofa table"],["lr_tube_tv","lr","TV (tube)"],["lr_flat_tv","lr","TV (flat panel)"],
    ["lr_misc_sm","lr","Miscellaneous (small)"],["lr_misc","lr","Miscellaneous"],

    ["dr_buffet_btm","dr","Buffet (bottom)"],["dr_buffet_top","dr","Buffet (top / hutch)"],
    ["dr_cabinet_crnr","dr","Corner cabinet"],["dr_chair","dr","Dining chair"],["dr_server","dr","Server"],
    ["dr_table","dr","Dining table"],["dr_tea_cart","dr","Tea cart"],

    ["kit_bakers_rack","kit","Baker's rack"],["kit_chair","kit","Kitchen chair"],["kit_table","kit","Breakfast table"],
    ["kit_high_chair","kit","High chair"],["kit_micro","kit","Microwave"],["kit_stool","kit","Stool"],
    ["kit_table_sm","kit","Small table"],["kit_util_cart","kit","Utility cart"],

    ["br_armoire","br","Armoire"],["br_armoire_lg","br","Armoire (large)"],["br_bed_bunk","br","Bunk bed"],
    ["br_bed_dbl","br","Bed (double / full)"],["br_bed_kq","br","Bed (king / queen)"],["br_bed_sngl","br","Bed (single / twin)"],
    ["br_chest","br","Chest"],["br_chair_bdr","br","Bedroom chair"],["br_chair","br","Chair"],
    ["br_chest_drs","br","Chest of drawers"],["br_hamper","br","Hamper"],["br_dresser_dbl","br","Dresser (double)"],
    ["br_xbike","br","Exercise bike"],["br_mirror","br","Mirror"],["br_nightstand","br","Nightstand"],
    ["br_treadmill","br","Treadmill"],["br_vanity","br","Vanity"],["br_misc_sm","br","Miscellaneous (small)"],
    ["br_misc","br","Miscellaneous"],

    ["ofc_bookcase_lg","ofc","Bookcase (large)"],["ofc_bookcase_sm","ofc","Bookcase (small)"],
    ["ofc_desk_sm","ofc","Desk (small)"],["ofc_desk_lg","ofc","Desk (large / exec)"],
    ["ofc_file_cab_lg","ofc","File cabinet (4-drawer)"],["ofc_file_cab_sm","ofc","File cabinet (2-drawer)"],
    ["ofc_machine","ofc","Office machine (copier, etc.)"],["ofc_sofa","ofc","Office sofa"],
    ["ofc_table_conf","ofc","Conference table"],["ofc_table_fold","ofc","Folding table"],
    ["ofc_table_sm","ofc","Small table"],["ofc_util_cart","ofc","Utility cart"],

    ["grg_ladder_ext","grg","Extension ladder"],["grg_ladder","grg","Step ladder"],["grg_mower","grg","Lawn mower"],
    ["grg_bike","grg","Bicycle"],["grg_cabinet_stg","grg","Storage cabinet"],["grg_chair_fold","grg","Folding chair"],
    ["grg_footlocker","grg","Footlocker"],["grg_shelves_mtl","grg","Metal shelves"],["grg_power_tools","grg","Power tools (group)"],
    ["grg_table_util","grg","Utility table"],["grg_tool_chest","grg","Tool chest"],["grg_trash_can","grg","Trash can"],
    ["grg_trunk","grg","Trunk"],["grg_bench_work","grg","Workbench"],["grg_misc","grg","Miscellaneous"],

    ["apl_fridge_top","apl","Refrigerator (top/bottom)"],["apl_fridge_sxs","apl","Refrigerator (side-by-side)"],
    ["apl_freezer","apl","Freezer"],["apl_washer","apl","Washer"],["apl_dryer","apl","Dryer"],
    ["apl_dishwasher","apl","Dishwasher"],["apl_stove","apl","Stove / range"],

    ["blky_piano_baby_grand","blky","Piano — baby grand"],["blky_piano_spinet","blky","Piano — spinet"],
    ["blky_piano_upright","blky","Piano — upright"],["blky_big_screen_tv","blky","Big-screen TV (60″+)"],
    ["blky_pool_table","blky","Pool table"],["blky_pinball","blky","Pinball / arcade"],

    ["pto_bbq","pto","BBQ grill"],["pto_chair_lawn","pto","Lawn chair"],["pto_chair_lng","pto","Lounge chair"],
    ["pto_hose_tools","pto","Hose / yard tools"],["pto_glider","pto","Glider"],["pto_picnic_tbl","pto","Picnic table"],
    ["pto_picnic_bch","pto","Picnic bench"],["pto_table_patio","pto","Patio table"],
    ["pto_umbrella","pto","Umbrella"],["pto_misc","pto","Miscellaneous"],

    ["misc_fan","misc","Fan / heater"],["misc_iron_board","misc","Ironing board"],
    ["misc_plant_lg","misc","Plant (large)"],["misc_plant_sm","misc","Plant (small)"],
    ["misc_rug_sm","misc","Rug (small)"],["misc_rug_lg","misc","Rug (large)"],
    ["misc_suitcase","misc","Suitcase"],["misc_misc","misc","Miscellaneous"]
  ];

  // ---- Render category sections ---------------------------------------------------------------
  var html = "";
  for (var i = 0; i < CATEGORIES.length; i++) {
    var cat = CATEGORIES[i];
    var open = i < 4 ? " open" : ""; // first 4 expanded by default; rest collapsed
    html += '<details class="iq__category"' + open + '>';
    html += '<summary>' + escapeHtml(cat.name) + '</summary>';
    html += '<div class="iq__items">';
    for (var j = 0; j < ITEMS.length; j++) {
      if (ITEMS[j][1] !== cat.key) continue;
      var key = ITEMS[j][0];
      var name = ITEMS[j][2];
      html += '<label class="iq__item" for="' + key + '">' +
              '<span class="iq__item-name">' + escapeHtml(name) + '</span>' +
              '<input id="' + key + '" type="number" name="' + key + '" min="0" value="" placeholder="0" data-iq-recalc />' +
              '</label>';
    }
    html += '</div></details>';
  }
  categoriesContainer.innerHTML = html;

  // ---- Live recalculation ---------------------------------------------------------------------
  var debounceTimer = null;
  var inflightController = null;
  var currencyFormatter = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" });
  var numberFormatter = new Intl.NumberFormat("en-US");

  function debounced(fn, ms) {
    return function () {
      clearTimeout(debounceTimer);
      debounceTimer = setTimeout(fn, ms);
    };
  }

  function collectRequest() {
    var items = [];
    var inputs = categoriesContainer.querySelectorAll('input[type="number"]');
    for (var i = 0; i < inputs.length; i++) {
      var qty = parseInt(inputs[i].value, 10);
      if (qty > 0) items.push({ key: inputs[i].name, quantity: qty });
    }
    return {
      firstName: form.first_name && form.first_name.value || "",
      lastName: form.last_name && form.last_name.value || "",
      phone: form.phone && form.phone.value || "",
      email: form.email && form.email.value || "",
      moveDate: form.move_date && form.move_date.value || null,
      fromStreet: form.from_street.value, fromCity: form.from_city.value, fromState: form.from_state.value, fromZip: form.from_zip.value,
      fromHomeType: form.from_home_type.value, fromBedrooms: form.from_bedrooms.value, fromFloor: form.from_floor.value,
      toStreet: form.to_street.value, toCity: form.to_city.value, toState: form.to_state.value, toZip: form.to_zip.value,
      toHomeType: form.to_home_type.value, toBedrooms: form.to_bedrooms.value, toFloor: form.to_floor.value,
      miles: parseInt(form.miles.value, 10) || 0,
      items: items,
      smallBoxes: parseInt(form.box_small.value, 10) || 0,
      mediumBoxes: parseInt(form.box_medium.value, 10) || 0,
      largeBoxes: parseInt(form.box_large.value, 10) || 0,
      wardrobeBoxes: parseInt(form.box_wardrobe.value, 10) || 0,
      notes: form.notes && form.notes.value || ""
    };
  }

  function recalc() {
    var req = collectRequest();
    var totalQty = req.items.reduce(function (a, b) { return a + b.quantity; }, 0) + req.smallBoxes + req.mediumBoxes + req.largeBoxes + req.wardrobeBoxes;
    if (totalQty === 0) {
      priceEl.textContent = "—";
      statusEl.textContent = "Add a few items to see your price.";
      statusEl.hidden = false;
      statsEl.hidden = true;
      return;
    }

    // First name placeholder for the API which requires it; live calc doesn't validate contact
    req.firstName = req.firstName || "Anonymous";
    req.lastName = req.lastName || "";
    req.phone = req.phone || "000";
    req.email = req.email || "live@example.com";

    if (inflightController) inflightController.abort();
    inflightController = new AbortController();

    fetch("/api/quote/calculate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
      signal: inflightController.signal
    })
      .then(function (r) { return r.ok ? r.json() : Promise.reject(new Error("HTTP " + r.status)); })
      .then(renderQuote)
      .catch(function (err) {
        if (err.name === "AbortError") return;
        statusEl.textContent = "Couldn’t calculate just now. Try again or call (818) 884-6125.";
      });
  }

  function renderQuote(q) {
    priceEl.textContent = currencyFormatter.format(q.totalPrice);
    statusEl.hidden = true;
    statsEl.hidden = false;
    fields.pieces.textContent = numberFormatter.format(q.totalPieces);
    fields.cuft.textContent = numberFormatter.format(Math.round(q.totalCubicFeet)) + " cu ft";
    fields.crew.textContent = q.crewSize + " movers";
    fields.hours.textContent = formatHours(q.totalBillableHours);
    fields.drive.textContent = formatHours(q.driveTimeHours);
    fields.fuel.textContent = currencyFormatter.format(q.fuelSurcharge);
  }

  function formatHours(h) {
    var whole = Math.floor(h);
    var mins = Math.round((h - whole) * 60);
    if (mins === 0) return whole + " hr";
    return whole + " hr " + mins + " min";
  }

  function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }

  // Wire all data-iq-recalc inputs (including ones we just rendered) to the debounced recalc
  var trigger = debounced(recalc, 250);
  form.addEventListener("input", function (e) {
    if (e.target && e.target.matches && e.target.matches("[data-iq-recalc], #iq-categories input")) {
      trigger();
    }
  });
})();
