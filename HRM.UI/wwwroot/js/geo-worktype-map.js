(function () {
    const maps = new Map();
    let leafletPromise = null;

    function ensureLeaflet() {
        if (window.L) return Promise.resolve(window.L);
        if (leafletPromise) return leafletPromise;

        leafletPromise = new Promise((resolve, reject) => {
            if (!document.getElementById("hrm-leaflet-css")) {
                const link = document.createElement("link");
                link.id = "hrm-leaflet-css";
                link.rel = "stylesheet";
                link.href = "https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/leaflet.css";
                link.crossOrigin = "";
                document.head.appendChild(link);
            }

            const existingScript = document.getElementById("hrm-leaflet-js");
            if (existingScript) {
                existingScript.addEventListener("load", () => resolve(window.L));
                existingScript.addEventListener("error", () => reject(new Error("Map library could not be loaded.")));
                return;
            }

            const script = document.createElement("script");
            script.id = "hrm-leaflet-js";
            script.src = "https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/leaflet.js";
            script.crossOrigin = "";
            script.onload = () => {
                if (window.L) resolve(window.L);
                else reject(new Error("Map library failed to initialize."));
            };
            script.onerror = () => reject(new Error("Could not load map tiles library. You can still enter coordinates manually or use your current location."));
            document.head.appendChild(script);
        });

        return leafletPromise;
    }

    async function init(elementId, lat, lng, radiusMeters, dotNetRef) {
        const container = document.getElementById(elementId);
        if (!container) return { ok: false, error: "Map container not found." };

        try {
            const L = await ensureLeaflet();
            destroy(elementId);

            const startLat = Number.isFinite(lat) ? lat : 4.1755;
            const startLng = Number.isFinite(lng) ? lng : 73.5093;
            const radius = Math.max(25, Math.min(5000, Number(radiusMeters) || 150));

            const map = L.map(container, {
                center: [startLat, startLng],
                zoom: 16,
                zoomControl: true
            });

            L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 19,
                attribution: "&copy; OpenStreetMap contributors"
            }).addTo(map);

            const marker = L.marker([startLat, startLng], { draggable: true }).addTo(map);
            const circle = L.circle([startLat, startLng], {
                radius: radius,
                color: "#059669",
                fillColor: "#10b981",
                fillOpacity: 0.18,
                weight: 2
            }).addTo(map);

            function notify(newLat, newLng) {
                const roundedLat = Number(newLat.toFixed(6));
                const roundedLng = Number(newLng.toFixed(6));
                marker.setLatLng([roundedLat, roundedLng]);
                circle.setLatLng([roundedLat, roundedLng]);
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync("OnMapCoordinatesSelected", roundedLat, roundedLng).catch(() => { });
                }
            }

            map.on("click", function (e) {
                notify(e.latlng.lat, e.latlng.lng);
            });

            marker.on("dragend", function () {
                const pos = marker.getLatLng();
                notify(pos.lat, pos.lng);
            });

            maps.set(elementId, { map, marker, circle });

            setTimeout(() => {
                try { map.invalidateSize(); } catch { }
            }, 150);

            return { ok: true, error: null };
        } catch (err) {
            return { ok: false, error: err && err.message ? err.message : "Map could not be loaded." };
        }
    }

    function update(elementId, lat, lng, radiusMeters) {
        const entry = maps.get(elementId);
        if (!entry) return;
        const targetLat = Number(lat);
        const targetLng = Number(lng);
        const radius = Math.max(25, Math.min(5000, Number(radiusMeters) || 150));
        if (!Number.isFinite(targetLat) || !Number.isFinite(targetLng)) return;

        entry.marker.setLatLng([targetLat, targetLng]);
        entry.circle.setLatLng([targetLat, targetLng]);
        entry.circle.setRadius(radius);
        entry.map.panTo([targetLat, targetLng], { animate: true });
    }

    function destroy(elementId) {
        const entry = maps.get(elementId);
        if (entry) {
            try { entry.map.remove(); } catch { }
            maps.delete(elementId);
        }
    }

    function getCurrentPosition() {
        return new Promise((resolve) => {
            if (!navigator.geolocation) {
                resolve({ ok: false, lat: 0, lng: 0, accuracy: 0, error: "Geolocation is not supported by this browser." });
                return;
            }
            navigator.geolocation.getCurrentPosition(
                (pos) => {
                    resolve({
                        ok: true,
                        lat: Number(pos.coords.latitude.toFixed(6)),
                        lng: Number(pos.coords.longitude.toFixed(6)),
                        accuracy: Math.round(pos.coords.accuracy || 0),
                        error: null
                    });
                },
                (err) => {
                    let msg = "Unable to retrieve your current location.";
                    if (err && err.code === 1) msg = "Location permission was denied. Please allow location access in your browser.";
                    else if (err && err.code === 2) msg = "Location information is currently unavailable.";
                    else if (err && err.code === 3) msg = "Location request timed out. Please try again.";
                    resolve({ ok: false, lat: 0, lng: 0, accuracy: 0, error: msg });
                },
                { enableHighAccuracy: true, timeout: 12000, maximumAge: 10000 }
            );
        });
    }

    window.hrmGeoMap = { init, update, destroy, getCurrentPosition };
})();
