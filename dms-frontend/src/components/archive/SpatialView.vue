<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { PhysicalLocation, PhysicalItem } from '@/types'

const props = defineProps<{
  locations: PhysicalLocation[]
  items: PhysicalItem[]
  selectedLocationId: string | null
}>()

const emit = defineEmits<{
  select: [locationId: string]
  selectItem: [itemId: string]
}>()

// ── Navigation State ──
const navigationStack = ref<string[]>([])
const currentParentId = computed<string | null>(() =>
  navigationStack.value.length > 0
    ? navigationStack.value[navigationStack.value.length - 1]
    : null
)

// ── Tooltip ──
const tooltip = ref<{ show: boolean; x: number; y: number; text: string; sub: string }>({
  show: false, x: 0, y: 0, text: '', sub: ''
})

// ── Location hierarchy helpers ──
const locationMap = computed(() => {
  const map = new Map<string, PhysicalLocation>()
  for (const loc of props.locations) map.set(loc.id, loc)
  return map
})

const currentChildren = computed(() =>
  props.locations
    .filter(l => {
      if (currentParentId.value === null) return !l.parentId
      return l.parentId === currentParentId.value
    })
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name))
)

const currentLocation = computed(() =>
  currentParentId.value ? locationMap.value.get(currentParentId.value) ?? null : null
)

// Determine visualization level based on current location type or root
const currentLevel = computed<string>(() => {
  if (!currentLocation.value) return 'root'
  const type = currentLocation.value.locationType
  if (type === 'Site') return 'site'
  if (type === 'Building') return 'building'
  if (type === 'Floor') return 'floor'
  if (type === 'Room') return 'room'
  return 'leaf'
})

// Items at current leaf level
const itemsAtCurrentLevel = computed(() => {
  if (currentChildren.value.length > 0) return []
  if (!currentParentId.value) return []
  return props.items.filter(i => i.locationId === currentParentId.value)
})

// ── Breadcrumbs ──
const breadcrumbs = computed(() => {
  const crumbs: { id: string | null; label: string }[] = [{ id: null, label: 'Overview' }]
  for (const locId of navigationStack.value) {
    const loc = locationMap.value.get(locId)
    if (loc) crumbs.push({ id: locId, label: loc.name })
  }
  return crumbs
})

// ── Navigation ──
function drillDown(locationId: string) {
  navigationStack.value = [...navigationStack.value, locationId]
  emit('select', locationId)
}

function navigateTo(id: string | null) {
  if (id === null) {
    navigationStack.value = []
    return
  }
  const idx = navigationStack.value.indexOf(id)
  if (idx >= 0) {
    navigationStack.value = navigationStack.value.slice(0, idx + 1)
  }
}

// ── Capacity helpers ──
function capacityPercent(loc: PhysicalLocation): number {
  if (!loc.capacity || loc.capacity === 0) return 0
  return Math.min(100, Math.round((loc.currentCount / loc.capacity) * 100))
}

function capacityColor(loc: PhysicalLocation): string {
  const pct = capacityPercent(loc)
  if (pct >= 100) return 'capacity-full'
  if (pct > 90) return 'capacity-critical'
  if (pct > 70) return 'capacity-warning'
  return 'capacity-ok'
}

function capacityColorHex(loc: PhysicalLocation): string {
  const pct = capacityPercent(loc)
  if (pct >= 100) return '#ef4444'
  if (pct > 90) return '#f43f5e'
  if (pct > 70) return '#f59e0b'
  return '#14b8a6'
}

function childCount(loc: PhysicalLocation): number {
  return props.locations.filter(l => l.parentId === loc.id).length +
    props.items.filter(i => i.locationId === loc.id).length
}

// Building height proportional to children
function buildingHeight(loc: PhysicalLocation): number {
  const count = childCount(loc)
  return Math.max(80, Math.min(180, 60 + count * 15))
}

// ── Condition colors for items ──
const conditionColorMap: Record<string, string> = {
  Good: '#22c55e',
  Fair: '#eab308',
  Poor: '#f97316',
  Damaged: '#ef4444',
  Destroyed: '#6b7280'
}

// ── Tooltip handlers ──
function showTooltip(e: MouseEvent, text: string, sub: string) {
  tooltip.value = { show: true, x: e.clientX, y: e.clientY - 10, text, sub }
}

function hideTooltip() {
  tooltip.value.show = false
}

// Reset navigation when locations change substantially
watch(() => props.locations.length, () => {
  // Validate navigation stack - remove invalid entries
  const valid = navigationStack.value.filter(id => locationMap.value.has(id))
  if (valid.length !== navigationStack.value.length) {
    navigationStack.value = valid
  }
})

const locationTypeIcons: Record<string, string> = {
  Site: 'domain',
  Building: 'apartment',
  Floor: 'layers',
  Room: 'meeting_room',
  Rack: 'view_column',
  Shelf: 'shelves',
  Box: 'package_2',
  File: 'folder'
}

function getIcon(type: string): string {
  return locationTypeIcons[type] || 'location_on'
}
</script>

<template>
  <div class="spatial-root">
    <!-- Breadcrumb Navigation -->
    <div class="spatial-breadcrumbs">
      <template v-for="(crumb, i) in breadcrumbs" :key="crumb.id ?? 'root'">
        <span v-if="i > 0" class="breadcrumb-sep">/</span>
        <button
          @click="navigateTo(crumb.id)"
          class="breadcrumb-item"
          :class="{ active: i === breadcrumbs.length - 1 }"
        >
          <span v-if="i === 0" class="material-symbols-outlined text-sm mr-1">warehouse</span>
          {{ crumb.label }}
        </button>
      </template>
      <span class="breadcrumb-count">{{ currentChildren.length }} locations{{ itemsAtCurrentLevel.length ? `, ${itemsAtCurrentLevel.length} items` : '' }}</span>
    </div>

    <!-- Scene Container -->
    <div class="spatial-scene">
      <!-- Background Grid Pattern -->
      <svg class="scene-grid" width="100%" height="100%">
        <defs>
          <pattern id="sp-grid-small" width="20" height="20" patternUnits="userSpaceOnUse">
            <path d="M 20 0 L 0 0 0 20" fill="none" class="grid-line-light" stroke-width="0.5" />
          </pattern>
          <pattern id="sp-grid-large" width="100" height="100" patternUnits="userSpaceOnUse">
            <rect width="100" height="100" fill="url(#sp-grid-small)" />
            <path d="M 100 0 L 0 0 0 100" fill="none" class="grid-line-strong" stroke-width="1" />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill="url(#sp-grid-large)" />
      </svg>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- ROOT / SITE LEVEL: Isometric 3D Buildings      -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-if="currentLevel === 'root' || currentLevel === 'site'" class="viz-container">
        <div v-if="currentChildren.length === 0" class="empty-state">
          <span class="material-symbols-outlined text-4xl text-gray-600 mb-3">domain_add</span>
          <p class="text-gray-500 text-sm">No locations at this level</p>
        </div>
        <div v-else class="isometric-scene">
          <div class="isometric-container">
            <div
              v-for="(loc, idx) in currentChildren"
              :key="loc.id"
              class="building-wrapper"
              :style="{
                '--idx': idx,
                '--total': currentChildren.length,
                '--height': buildingHeight(loc) + 'px',
                '--color': capacityColorHex(loc)
              }"
              @click="drillDown(loc.id)"
              @mouseenter="showTooltip($event, loc.name, `${loc.locationType} · ${loc.currentCount}/${loc.capacity ?? '∞'} · ${childCount(loc)} children`)"
              @mouseleave="hideTooltip"
            >
              <div class="building-block" :class="capacityColor(loc)">
                <div class="building-top"></div>
                <div class="building-front">
                  <div class="building-label">
                    <span class="material-symbols-outlined text-lg mb-1">{{ getIcon(loc.locationType) }}</span>
                    <span class="building-name">{{ loc.name }}</span>
                    <span class="building-code">{{ loc.code }}</span>
                  </div>
                  <!-- Windows -->
                  <div class="building-windows">
                    <div v-for="n in Math.min(6, childCount(loc))" :key="n" class="building-window"></div>
                  </div>
                </div>
                <div class="building-right"></div>
              </div>
              <!-- Capacity bar underneath -->
              <div class="building-capacity-bar">
                <div class="building-capacity-fill" :style="{ width: capacityPercent(loc) + '%' }"></div>
              </div>
              <div class="building-capacity-text">{{ loc.currentCount }}/{{ loc.capacity ?? '∞' }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- BUILDING LEVEL: Stacked Floor Layers            -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="currentLevel === 'building'" class="viz-container">
        <div v-if="currentChildren.length === 0" class="empty-state">
          <span class="material-symbols-outlined text-4xl text-gray-600 mb-3">layers</span>
          <p class="text-gray-500 text-sm">No floors in this building</p>
        </div>
        <div v-else class="floors-stack">
          <div
            v-for="(loc, idx) in [...currentChildren].reverse()"
            :key="loc.id"
            class="floor-layer"
            :class="capacityColor(loc)"
            :style="{ '--floor-idx': idx, '--color': capacityColorHex(loc) }"
            @click="drillDown(loc.id)"
            @mouseenter="showTooltip($event, loc.name, `${loc.locationType} · ${loc.currentCount}/${loc.capacity ?? '∞'}`)"
            @mouseleave="hideTooltip"
          >
            <div class="floor-front">
              <div class="floor-label">
                <span class="material-symbols-outlined text-base mr-2">{{ getIcon(loc.locationType) }}</span>
                <span class="floor-name">{{ loc.name }}</span>
                <span class="floor-code">{{ loc.code }}</span>
              </div>
              <div class="floor-capacity">
                <div class="floor-capacity-bar">
                  <div class="floor-capacity-fill" :style="{ width: capacityPercent(loc) + '%' }"></div>
                </div>
                <span class="floor-capacity-text">{{ loc.currentCount }}/{{ loc.capacity ?? '∞' }}</span>
              </div>
            </div>
            <div class="floor-top"></div>
            <div class="floor-side"></div>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- FLOOR LEVEL: Room Grid (Floor Plan)             -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="currentLevel === 'floor'" class="viz-container">
        <div v-if="currentChildren.length === 0" class="empty-state">
          <span class="material-symbols-outlined text-4xl text-gray-600 mb-3">meeting_room</span>
          <p class="text-gray-500 text-sm">No rooms on this floor</p>
        </div>
        <div v-else class="floor-plan">
          <div
            v-for="loc in currentChildren"
            :key="loc.id"
            class="room-cell"
            :class="capacityColor(loc)"
            @click="drillDown(loc.id)"
            @mouseenter="showTooltip($event, loc.name, `${loc.locationType} · ${loc.currentCount}/${loc.capacity ?? '∞'}`)"
            @mouseleave="hideTooltip"
          >
            <div class="room-icon">
              <span class="material-symbols-outlined">{{ getIcon(loc.locationType) }}</span>
            </div>
            <div class="room-name">{{ loc.name }}</div>
            <div class="room-code">{{ loc.code }}</div>
            <div class="room-capacity-bar">
              <div class="room-capacity-fill" :style="{ width: capacityPercent(loc) + '%', background: capacityColorHex(loc) }"></div>
            </div>
            <div class="room-count">{{ loc.currentCount }}/{{ loc.capacity ?? '∞' }}</div>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- ROOM LEVEL: Rack Shelving Units                 -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="currentLevel === 'room'" class="viz-container">
        <div v-if="currentChildren.length === 0 && itemsAtCurrentLevel.length === 0" class="empty-state">
          <span class="material-symbols-outlined text-4xl text-gray-600 mb-3">view_column</span>
          <p class="text-gray-500 text-sm">No racks in this room</p>
        </div>
        <div v-else class="rack-room">
          <div
            v-for="loc in currentChildren"
            :key="loc.id"
            class="rack-unit"
            :class="capacityColor(loc)"
            @click="drillDown(loc.id)"
            @mouseenter="showTooltip($event, loc.name, `${loc.locationType} · ${loc.currentCount}/${loc.capacity ?? '∞'}`)"
            @mouseleave="hideTooltip"
          >
            <div class="rack-header">
              <span class="material-symbols-outlined text-sm">{{ getIcon(loc.locationType) }}</span>
              <span class="rack-name">{{ loc.name }}</span>
            </div>
            <div class="rack-body">
              <div class="rack-shelves">
                <div
                  v-for="n in Math.max(3, Math.min(6, Math.ceil((loc.capacity ?? 20) / 5)))"
                  :key="n"
                  class="rack-shelf-row"
                >
                  <div class="shelf-divider"></div>
                </div>
              </div>
            </div>
            <div class="rack-footer">
              <div class="rack-capacity-bar">
                <div class="rack-capacity-fill" :style="{ width: capacityPercent(loc) + '%', background: capacityColorHex(loc) }"></div>
              </div>
              <span class="rack-count">{{ loc.currentCount }}/{{ loc.capacity ?? '∞' }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- LEAF LEVEL: Item Blocks on Shelves              -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else class="viz-container">
        <div v-if="currentChildren.length === 0 && itemsAtCurrentLevel.length === 0" class="empty-state">
          <span class="material-symbols-outlined text-4xl text-gray-600 mb-3">inventory_2</span>
          <p class="text-gray-500 text-sm">No items at this location</p>
        </div>

        <!-- Sub-locations if any -->
        <div v-if="currentChildren.length > 0" class="leaf-locations">
          <div
            v-for="loc in currentChildren"
            :key="loc.id"
            class="leaf-loc-card"
            :class="capacityColor(loc)"
            @click="drillDown(loc.id)"
          >
            <span class="material-symbols-outlined text-lg">{{ getIcon(loc.locationType) }}</span>
            <span class="leaf-loc-name">{{ loc.name }}</span>
            <span class="leaf-loc-count">{{ loc.currentCount }}/{{ loc.capacity ?? '∞' }}</span>
          </div>
        </div>

        <!-- Items -->
        <div v-if="itemsAtCurrentLevel.length > 0" class="item-shelf">
          <div class="shelf-label">
            <span class="material-symbols-outlined text-sm">inventory_2</span>
            Items ({{ itemsAtCurrentLevel.length }})
          </div>
          <div class="item-blocks">
            <div
              v-for="item in itemsAtCurrentLevel"
              :key="item.id"
              class="item-block"
              :style="{ '--item-color': conditionColorMap[item.condition] || '#6b7280' }"
              @click="emit('selectItem', item.id)"
              @mouseenter="showTooltip($event, item.title, `${item.barcode} · ${item.condition} · ${item.itemType}`)"
              @mouseleave="hideTooltip"
            >
              <div class="item-block-color"></div>
              <div class="item-block-label">{{ item.title.substring(0, 20) }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Legend -->
    <div class="spatial-legend">
      <span class="legend-title">Capacity:</span>
      <span class="legend-item"><span class="legend-dot bg-teal-500"></span> &lt;70%</span>
      <span class="legend-item"><span class="legend-dot bg-amber-500"></span> 70-90%</span>
      <span class="legend-item"><span class="legend-dot bg-rose-500"></span> &gt;90%</span>
      <span class="legend-item"><span class="legend-dot bg-red-500 animate-pulse"></span> Full</span>
      <span class="legend-sep">|</span>
      <span class="legend-title">Condition:</span>
      <span class="legend-item"><span class="legend-dot" style="background:#22c55e"></span> Good</span>
      <span class="legend-item"><span class="legend-dot" style="background:#eab308"></span> Fair</span>
      <span class="legend-item"><span class="legend-dot" style="background:#f97316"></span> Poor</span>
      <span class="legend-item"><span class="legend-dot" style="background:#ef4444"></span> Damaged</span>
    </div>

    <!-- Tooltip -->
    <Teleport to="body">
      <Transition name="tooltip-fade">
        <div
          v-if="tooltip.show"
          class="spatial-tooltip"
          :style="{ left: tooltip.x + 'px', top: (tooltip.y - 8) + 'px' }"
        >
          <div class="tooltip-text">{{ tooltip.text }}</div>
          <div class="tooltip-sub">{{ tooltip.sub }}</div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
/* ══════════════════════════════════════════════ */
/* CSS Custom Properties — Light (default)        */
/* ══════════════════════════════════════════════ */
.spatial-root {
  --sp-bg-chrome: #f8fafc;
  --sp-bg-scene: #f1f5f9;
  --sp-border: #e2e8f0;
  --sp-border-subtle: #e2e8f0;
  --sp-text-primary: #1e293b;
  --sp-text-secondary: #64748b;
  --sp-text-muted: #94a3b8;
  --sp-text-faint: #cbd5e1;
  --sp-surface: white;
  --sp-surface-hover: #f8fafc;
  --sp-surface-tint: rgba(0, 0, 0, 0.02);
  --sp-surface-tint-hover: rgba(0, 0, 0, 0.04);
  --sp-grid-line: rgba(0, 0, 0, 0.04);
  --sp-grid-line-strong: rgba(0, 0, 0, 0.07);
  --sp-building-bg-mix: #f1f5f9;
  --sp-building-side-mix: #e2e8f0;
  --sp-building-dark-mix: #cbd5e1;
  --sp-shadow: rgba(0, 0, 0, 0.08);
  --sp-shadow-heavy: rgba(0, 0, 0, 0.12);
  --sp-capacity-bar-bg: #e2e8f0;
  --sp-tooltip-bg: white;
  --sp-tooltip-border: #e2e8f0;
  --sp-tooltip-shadow: rgba(0, 0, 0, 0.12);
  --sp-window-bg: rgba(0, 0, 0, 0.05);
  --sp-window-border: rgba(0, 0, 0, 0.08);
  --sp-shelf-line: linear-gradient(90deg, rgba(0,0,0,0.06), rgba(0,0,0,0.12), rgba(0,0,0,0.06));
}

/* ── Dark mode overrides ── */
:root.dark .spatial-root {
  --sp-bg-chrome: #0d1117;
  --sp-bg-scene: #080b10;
  --sp-border: rgba(255, 255, 255, 0.06);
  --sp-border-subtle: rgba(255, 255, 255, 0.06);
  --sp-text-primary: rgba(255, 255, 255, 0.85);
  --sp-text-secondary: rgba(255, 255, 255, 0.5);
  --sp-text-muted: rgba(255, 255, 255, 0.35);
  --sp-text-faint: rgba(255, 255, 255, 0.2);
  --sp-surface: rgba(255, 255, 255, 0.03);
  --sp-surface-hover: rgba(255, 255, 255, 0.05);
  --sp-surface-tint: rgba(255, 255, 255, 0.04);
  --sp-surface-tint-hover: rgba(255, 255, 255, 0.08);
  --sp-grid-line: rgba(255, 255, 255, 0.03);
  --sp-grid-line-strong: rgba(255, 255, 255, 0.06);
  --sp-building-bg-mix: #1a1f2e;
  --sp-building-side-mix: #0d1117;
  --sp-building-dark-mix: #060810;
  --sp-shadow: rgba(0, 0, 0, 0.3);
  --sp-shadow-heavy: rgba(0, 0, 0, 0.5);
  --sp-capacity-bar-bg: rgba(255, 255, 255, 0.08);
  --sp-tooltip-bg: #1a1f2e;
  --sp-tooltip-border: rgba(255, 255, 255, 0.1);
  --sp-tooltip-shadow: rgba(0, 0, 0, 0.5);
  --sp-window-bg: rgba(255, 255, 255, 0.06);
  --sp-window-border: rgba(255, 255, 255, 0.08);
  --sp-shelf-line: linear-gradient(90deg, rgba(255,255,255,0.08), rgba(255,255,255,0.15), rgba(255,255,255,0.08));
}

/* ══════════════════════════════════════════════ */
/* Root & Layout                                  */
/* ══════════════════════════════════════════════ */
.spatial-root {
  display: flex;
  flex-direction: column;
  border-radius: 0.75rem;
  overflow: hidden;
  border: 1px solid var(--sp-border);
}

/* ── Breadcrumbs ── */
.spatial-breadcrumbs {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: var(--sp-bg-chrome);
  border-bottom: 1px solid var(--sp-border);
  flex-wrap: wrap;
}

.breadcrumb-item {
  display: inline-flex;
  align-items: center;
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--sp-text-secondary);
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  border-radius: 0.375rem;
  transition: all 0.15s;
}

.breadcrumb-item:hover {
  color: #14b8a6;
  background: rgba(20, 184, 166, 0.1);
}

.breadcrumb-item.active {
  color: var(--sp-text-primary);
  cursor: default;
}

.breadcrumb-item.active:hover {
  background: none;
  color: var(--sp-text-primary);
}

.breadcrumb-sep {
  color: var(--sp-text-faint);
  font-size: 0.75rem;
}

.breadcrumb-count {
  margin-left: auto;
  font-size: 0.6875rem;
  color: var(--sp-text-muted);
  font-variant-numeric: tabular-nums;
}

/* ── Scene ── */
.spatial-scene {
  position: relative;
  background: var(--sp-bg-scene);
  min-height: 480px;
  overflow: hidden;
}

.scene-grid {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.grid-line-light { stroke: rgba(0, 0, 0, 0.04); }
.grid-line-strong { stroke: rgba(0, 0, 0, 0.07); }

:root.dark .grid-line-light { stroke: rgba(255, 255, 255, 0.03); }
:root.dark .grid-line-strong { stroke: rgba(255, 255, 255, 0.06); }

.viz-container {
  position: relative;
  z-index: 1;
  padding: 2rem;
  min-height: 480px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  opacity: 0.5;
}

/* ══════════════════════════════════════════════ */
/* Isometric Buildings (Root / Site Level)        */
/* ══════════════════════════════════════════════ */
.isometric-scene {
  width: 100%;
  perspective: 1200px;
  display: flex;
  justify-content: center;
  padding: 3rem 0;
}

.isometric-container {
  display: flex;
  gap: 3rem;
  transform: rotateX(55deg) rotateZ(-45deg);
  transform-style: preserve-3d;
}

.building-wrapper {
  cursor: pointer;
  position: relative;
  transform-style: preserve-3d;
  transition: transform 0.3s ease;
}

.building-wrapper:hover {
  transform: translateY(-8px);
}

.building-block {
  width: 120px;
  height: var(--height, 100px);
  position: relative;
  transform-style: preserve-3d;
}

.building-top {
  position: absolute;
  top: 0;
  left: 0;
  width: 120px;
  height: 120px;
  background: color-mix(in srgb, var(--color) 60%, var(--sp-building-bg-mix));
  transform: rotateX(90deg) translateZ(0px);
  transform-origin: top;
  border: 1px solid var(--sp-border-subtle);
}

.building-front {
  position: absolute;
  bottom: 0;
  left: 0;
  width: 120px;
  height: var(--height, 100px);
  background: linear-gradient(180deg, color-mix(in srgb, var(--color) 35%, var(--sp-building-side-mix)), color-mix(in srgb, var(--color) 15%, var(--sp-building-side-mix)));
  border: 1px solid var(--sp-border-subtle);
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0.75rem 0.5rem;
  overflow: hidden;
}

.building-right {
  position: absolute;
  bottom: 0;
  right: -60px;
  width: 60px;
  height: var(--height, 100px);
  background: color-mix(in srgb, var(--color) 20%, var(--sp-building-dark-mix));
  transform: skewY(-45deg);
  transform-origin: bottom left;
  border: 1px solid var(--sp-border-subtle);
}

.building-label {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.125rem;
  color: var(--sp-text-primary);
  text-align: center;
}

.building-name {
  font-size: 0.6875rem;
  font-weight: 600;
  line-height: 1.2;
  max-width: 100px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.building-code {
  font-size: 0.5625rem;
  color: var(--sp-text-muted);
  font-family: monospace;
}

.building-windows {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 4px;
  padding: 0 0.5rem;
  margin-top: auto;
}

.building-window {
  width: 100%;
  aspect-ratio: 1;
  background: var(--sp-window-bg);
  border: 1px solid var(--sp-window-border);
  border-radius: 1px;
}

.building-capacity-bar {
  width: 100px;
  height: 3px;
  background: var(--sp-capacity-bar-bg);
  border-radius: 2px;
  overflow: hidden;
  margin-top: 0.5rem;
}

.building-capacity-fill {
  height: 100%;
  background: var(--color);
  border-radius: 2px;
  transition: width 0.3s ease;
}

.building-capacity-text {
  font-size: 0.5625rem;
  color: var(--sp-text-muted);
  text-align: center;
  margin-top: 0.25rem;
  font-variant-numeric: tabular-nums;
}

/* ══════════════════════════════════════════════ */
/* Stacked Floors (Building Level)               */
/* ══════════════════════════════════════════════ */
.floors-stack {
  display: flex;
  flex-direction: column;
  gap: 0;
  perspective: 800px;
  width: 100%;
  max-width: 700px;
}

.floor-layer {
  position: relative;
  transform-style: preserve-3d;
  cursor: pointer;
  transition: all 0.2s ease;
}

.floor-layer:hover {
  transform: translateX(8px) translateZ(10px);
}

.floor-front {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.5rem;
  background: var(--sp-surface);
  border: 1px solid var(--sp-border);
  border-radius: 0.5rem;
  backdrop-filter: blur(4px);
  position: relative;
  z-index: 1;
}

.floor-layer:hover .floor-front {
  border-color: var(--color);
  background: color-mix(in srgb, var(--color) 8%, var(--sp-surface));
}

.floor-top {
  position: absolute;
  top: -6px;
  left: 6px;
  right: 0;
  height: 6px;
  background: color-mix(in srgb, var(--color) 25%, var(--sp-building-side-mix));
  border-radius: 0.25rem 0.25rem 0 0;
  transform: skewX(-45deg);
  transform-origin: bottom left;
}

.floor-side {
  position: absolute;
  top: -6px;
  right: -6px;
  bottom: 0;
  width: 6px;
  background: color-mix(in srgb, var(--color) 18%, var(--sp-building-dark-mix));
  border-radius: 0 0.25rem 0.25rem 0;
  transform: skewY(-45deg);
  transform-origin: top left;
}

.floor-label {
  display: flex;
  align-items: center;
  color: var(--sp-text-primary);
}

.floor-name {
  font-size: 0.875rem;
  font-weight: 600;
}

.floor-code {
  font-size: 0.6875rem;
  color: var(--sp-text-muted);
  font-family: monospace;
  margin-left: 0.75rem;
}

.floor-capacity {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.floor-capacity-bar {
  width: 100px;
  height: 4px;
  background: var(--sp-capacity-bar-bg);
  border-radius: 2px;
  overflow: hidden;
}

.floor-capacity-fill {
  height: 100%;
  background: var(--color);
  border-radius: 2px;
  transition: width 0.3s ease;
}

.floor-capacity-text {
  font-size: 0.6875rem;
  color: var(--sp-text-muted);
  font-variant-numeric: tabular-nums;
  min-width: 50px;
  text-align: right;
}

/* ══════════════════════════════════════════════ */
/* Floor Plan Grid (Floor Level)                  */
/* ══════════════════════════════════════════════ */
.floor-plan {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 1rem;
  width: 100%;
  max-width: 900px;
}

.room-cell {
  background: var(--sp-surface);
  border: 2px solid var(--sp-border);
  border-radius: 0.75rem;
  padding: 1.25rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.375rem;
  min-height: 140px;
}

.room-cell:hover {
  border-color: var(--color, #14b8a6);
  background: var(--sp-surface-hover);
  transform: translateY(-2px);
  box-shadow: 0 4px 20px var(--sp-shadow);
}

.room-cell.capacity-ok { border-left: 3px solid #14b8a6; }
.room-cell.capacity-warning { border-left: 3px solid #f59e0b; }
.room-cell.capacity-critical { border-left: 3px solid #f43f5e; }
.room-cell.capacity-full { border-left: 3px solid #ef4444; animation: pulse-border 2s infinite; }

.room-icon {
  width: 40px;
  height: 40px;
  border-radius: 0.625rem;
  background: var(--sp-surface-tint);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--sp-text-secondary);
}

.room-name {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--sp-text-primary);
  text-align: center;
  line-height: 1.2;
}

.room-code {
  font-size: 0.625rem;
  color: var(--sp-text-muted);
  font-family: monospace;
}

.room-capacity-bar {
  width: 100%;
  height: 3px;
  background: var(--sp-capacity-bar-bg);
  border-radius: 2px;
  overflow: hidden;
  margin-top: auto;
}

.room-capacity-fill {
  height: 100%;
  border-radius: 2px;
  transition: width 0.3s ease;
}

.room-count {
  font-size: 0.625rem;
  color: var(--sp-text-muted);
  font-variant-numeric: tabular-nums;
}

/* ══════════════════════════════════════════════ */
/* Rack Units (Room Level)                        */
/* ══════════════════════════════════════════════ */
.rack-room {
  display: flex;
  gap: 1.5rem;
  flex-wrap: wrap;
  justify-content: center;
  width: 100%;
}

.rack-unit {
  width: 140px;
  background: var(--sp-surface);
  border: 1px solid var(--sp-border);
  border-radius: 0.75rem;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.2s ease;
}

.rack-unit:hover {
  border-color: var(--sp-text-muted);
  transform: translateY(-3px);
  box-shadow: 0 8px 24px var(--sp-shadow);
}

.rack-header {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.625rem 0.75rem;
  background: var(--sp-surface-tint);
  border-bottom: 1px solid var(--sp-border);
  color: var(--sp-text-secondary);
}

.rack-name {
  font-size: 0.75rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rack-body {
  padding: 0.5rem;
}

.rack-shelves {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.rack-shelf-row {
  height: 20px;
  position: relative;
}

.shelf-divider {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: var(--sp-shelf-line);
  border-radius: 1px;
}

.rack-footer {
  padding: 0.5rem 0.75rem;
  border-top: 1px solid var(--sp-border);
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.rack-capacity-bar {
  flex: 1;
  height: 3px;
  background: var(--sp-capacity-bar-bg);
  border-radius: 2px;
  overflow: hidden;
}

.rack-capacity-fill {
  height: 100%;
  border-radius: 2px;
  transition: width 0.3s ease;
}

.rack-count {
  font-size: 0.625rem;
  color: var(--sp-text-muted);
  font-variant-numeric: tabular-nums;
}

/* Capacity classes for rack-unit styling */
.rack-unit.capacity-ok { border-top: 2px solid #14b8a6; }
.rack-unit.capacity-warning { border-top: 2px solid #f59e0b; }
.rack-unit.capacity-critical { border-top: 2px solid #f43f5e; }
.rack-unit.capacity-full { border-top: 2px solid #ef4444; }

/* ══════════════════════════════════════════════ */
/* Leaf Level (Items on Shelves)                  */
/* ══════════════════════════════════════════════ */
.leaf-locations {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  margin-bottom: 1.5rem;
  width: 100%;
}

.leaf-loc-card {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  background: var(--sp-surface);
  border: 1px solid var(--sp-border);
  border-radius: 0.5rem;
  cursor: pointer;
  color: var(--sp-text-secondary);
  transition: all 0.15s;
  font-size: 0.8125rem;
}

.leaf-loc-card:hover {
  background: var(--sp-surface-hover);
  border-color: var(--sp-text-muted);
}

.leaf-loc-name {
  font-weight: 500;
  color: var(--sp-text-primary);
}

.leaf-loc-count {
  font-size: 0.6875rem;
  color: var(--sp-text-muted);
  font-variant-numeric: tabular-nums;
}

.item-shelf {
  width: 100%;
}

.shelf-label {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--sp-text-secondary);
  margin-bottom: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.item-blocks {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.item-block {
  width: 110px;
  padding: 0.625rem;
  background: var(--sp-surface);
  border: 1px solid var(--sp-border);
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.15s;
  position: relative;
  overflow: hidden;
}

.item-block:hover {
  background: var(--sp-surface-hover);
  border-color: var(--sp-text-muted);
  transform: translateY(-1px);
}

.item-block-color {
  position: absolute;
  top: 0;
  left: 0;
  width: 3px;
  height: 100%;
  background: var(--item-color);
  border-radius: 3px 0 0 3px;
}

.item-block-label {
  font-size: 0.6875rem;
  color: var(--sp-text-secondary);
  padding-left: 0.25rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ══════════════════════════════════════════════ */
/* Legend                                          */
/* ══════════════════════════════════════════════ */
.spatial-legend {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.625rem 1.25rem;
  background: var(--sp-bg-chrome);
  border-top: 1px solid var(--sp-border);
  flex-wrap: wrap;
}

.legend-title {
  font-size: 0.625rem;
  font-weight: 700;
  color: var(--sp-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.1em;
}

.legend-item {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.6875rem;
  color: var(--sp-text-muted);
}

.legend-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.legend-sep {
  color: var(--sp-text-faint);
}

/* ══════════════════════════════════════════════ */
/* Tooltip                                        */
/* ══════════════════════════════════════════════ */
.spatial-tooltip {
  position: fixed;
  z-index: 9999;
  pointer-events: none;
  background: var(--sp-tooltip-bg);
  border: 1px solid var(--sp-tooltip-border);
  border-radius: 0.5rem;
  padding: 0.5rem 0.75rem;
  box-shadow: 0 8px 24px var(--sp-tooltip-shadow);
  transform: translate(-50%, -100%);
}

.tooltip-text {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--sp-text-primary);
  white-space: nowrap;
}

.tooltip-sub {
  font-size: 0.6875rem;
  color: var(--sp-text-secondary);
  margin-top: 0.125rem;
}

.tooltip-fade-enter-active,
.tooltip-fade-leave-active {
  transition: opacity 0.15s ease;
}

.tooltip-fade-enter-from,
.tooltip-fade-leave-to {
  opacity: 0;
}

/* ══════════════════════════════════════════════ */
/* Animations                                     */
/* ══════════════════════════════════════════════ */
@keyframes pulse-border {
  0%, 100% { border-color: #ef4444; }
  50% { border-color: rgba(239, 68, 68, 0.4); }
}
</style>
