// engine.js — Shared karting prototype library
// All modes load this via: <script src="../engine.js"></script>
// Exposes: window.Engine

(function () {
'use strict';

// ─── CONSTANTS ───────────────────────────────────────────────
const W = 1280, H = 720;
const STRAIGHT_LEFT = 280, STRAIGHT_RIGHT = 1000;
const TOP_Y = 200, BOTTOM_Y = 520, ARC_CY = 360;
const ARC_RADIUS = 160, TRACK_HALF_W = 70;
const INNER_R = ARC_RADIUS - TRACK_HALF_W;   // 90
const OUTER_R = ARC_RADIUS + TRACK_HALF_W;   // 230
const KART_LEN = 26, KART_WID = 14, KART_RADIUS = 11;
const COLORS = ['#42d6ff','#ffd042','#ff42d6','#42ff88','#d042ff','#ff8842'];
const PLAYER_COLOR = COLORS[0];
const MAX_SPEED = 240, ACCEL = 380, BRAKE_DECEL = 540;
const FRICTION_PER_S = 0.6, STEER_AT_SPEED = 3.2, REVERSE_FACTOR = 0.5;
const TRAIL_LEN = 30, NUM_WP = 80;

// ─── TRACK MATH ──────────────────────────────────────────────
function clampToTrack(x, y) {
  if (x >= STRAIGHT_LEFT && x <= STRAIGHT_RIGHT) {
    const ty = y < ARC_CY ? TOP_Y : BOTTOM_Y;
    const limit = TRACK_HALF_W - KART_RADIUS;
    const dy = y - ty;
    let hit = false;
    if (dy >  limit) { y = ty + limit; hit = true; }
    if (dy < -limit) { y = ty - limit; hit = true; }
    return { x, y, hit };
  }
  const cx = x > STRAIGHT_RIGHT ? STRAIGHT_RIGHT : STRAIGHT_LEFT;
  const dx = x - cx, dy = y - ARC_CY;
  let dist = Math.sqrt(dx * dx + dy * dy);
  const ang = Math.atan2(dy, dx);
  let hit = false;
  if (dist > OUTER_R - KART_RADIUS) { dist = OUTER_R - KART_RADIUS; hit = true; }
  else if (dist < INNER_R + KART_RADIUS) { dist = INNER_R + KART_RADIUS; hit = true; }
  return { x: cx + Math.cos(ang) * dist, y: ARC_CY + Math.sin(ang) * dist, hit };
}

function centerlineAt(t) {
  const sLen = STRAIGHT_RIGHT - STRAIGHT_LEFT;
  const aLen = Math.PI * ARC_RADIUS;
  const total = 2 * sLen + 2 * aLen;
  let s = (((t % 1) + 1) % 1) * total;
  if (s < sLen) return { x: STRAIGHT_LEFT + s, y: TOP_Y, heading: 0 };
  s -= sLen;
  if (s < aLen) {
    const a = -Math.PI / 2 + (s / aLen) * Math.PI;
    return { x: STRAIGHT_RIGHT + Math.cos(a) * ARC_RADIUS, y: ARC_CY + Math.sin(a) * ARC_RADIUS, heading: a + Math.PI / 2 };
  }
  s -= aLen;
  if (s < sLen) return { x: STRAIGHT_RIGHT - s, y: BOTTOM_Y, heading: Math.PI };
  s -= sLen;
  const a = Math.PI / 2 + (s / aLen) * Math.PI;
  return { x: STRAIGHT_LEFT + Math.cos(a) * ARC_RADIUS, y: ARC_CY + Math.sin(a) * ARC_RADIUS, heading: a + Math.PI / 2 };
}

const WAYPOINTS = [];
for (let i = 0; i < NUM_WP; i++) WAYPOINTS.push(centerlineAt(i / NUM_WP));

function nearestWaypointIndex(x, y) {
  let best = 0, bestD = Infinity;
  for (let i = 0; i < NUM_WP; i++) {
    const dx = WAYPOINTS[i].x - x, dy = WAYPOINTS[i].y - y;
    const d = dx * dx + dy * dy;
    if (d < bestD) { bestD = d; best = i; }
  }
  return best;
}

function spawnPositionForIndex(i, total) {
  const p = centerlineAt(0.04 + (i / total) * 0.18);
  const lat = (i % 2 === 0 ? -1 : 1) * (KART_RADIUS + 6);
  return { x: p.x - Math.sin(p.heading) * lat, y: p.y + Math.cos(p.heading) * lat, heading: p.heading };
}

// ─── KART ────────────────────────────────────────────────────
class Kart {
  constructor(id, isPlayer, color, spawn) {
    this.id = id;
    this.isPlayer = isPlayer;
    this.color = color;
    this.x = spawn.x; this.y = spawn.y;
    this.heading = spawn.heading;
    this.speed = 0;
    this.angVel = 0;
    this.trail = [];
    this.aiWP = nearestWaypointIndex(spawn.x, spawn.y);
    this.aiPersonality = 0.85 + Math.random() * 0.25;
    this.wallFlash = 0;
    this.maxSpeedMul = 1;
    this.steerMul = 1;
    this.tags = {};    // mode-specific state storage
  }
  topSpeed() {
    return MAX_SPEED * (this.isPlayer ? 1.0 : this.aiPersonality) * this.maxSpeedMul;
  }
}

// ─── INPUT ───────────────────────────────────────────────────
const keys = new Set();
window.addEventListener('keydown', e => keys.add(e.key.toLowerCase()));
window.addEventListener('keyup',   e => keys.delete(e.key.toLowerCase()));

function playerInput() {
  let throttle = 0, steer = 0;
  if (keys.has('w') || keys.has('arrowup'))    throttle += 1;
  if (keys.has('arrowdown'))                   throttle -= 1;
  if (keys.has('a') || keys.has('arrowleft'))  steer -= 1;
  if (keys.has('d') || keys.has('arrowright')) steer += 1;
  return { throttle, steer, action: keys.has(' ') || keys.has('e') };
}

// ─── AI ──────────────────────────────────────────────────────
function aiInputToward(kart, target, baseThrottle = 1) {
  const dx = target.x - kart.x, dy = target.y - kart.y;
  const desired = Math.atan2(dy, dx);
  let dh = desired - kart.heading;
  while (dh >  Math.PI) dh -= Math.PI * 2;
  while (dh < -Math.PI) dh += Math.PI * 2;
  return {
    steer:    Math.max(-1, Math.min(1, dh * 2.5)),
    throttle: baseThrottle - Math.min(0.55, Math.abs(dh) * 0.45),
  };
}

function advanceWaypoint(kart) {
  const wp = WAYPOINTS[kart.aiWP];
  const dx = wp.x - kart.x, dy = wp.y - kart.y;
  if (dx * dx + dy * dy < 50 * 50) kart.aiWP = (kart.aiWP + 1) % NUM_WP;
  return WAYPOINTS[kart.aiWP];
}

function nearestKart(self, karts, pred, maxR) {
  let best = null, bestD = maxR == null ? Infinity : maxR;
  for (const k of karts) {
    if (k === self || !pred(k)) continue;
    const dx = k.x - self.x, dy = k.y - self.y;
    const d = Math.sqrt(dx * dx + dy * dy);
    if (d < bestD) { best = k; bestD = d; }
  }
  return best;
}

// ─── PHYSICS ─────────────────────────────────────────────────
function stepKart(kart, dt, input) {
  if (input.throttle > 0)      kart.speed += ACCEL       * input.throttle * dt;
  else if (input.throttle < 0) kart.speed += BRAKE_DECEL * input.throttle * dt;
  else                         kart.speed *= Math.pow(1 - FRICTION_PER_S, dt);

  const top = kart.topSpeed();
  if (kart.speed >  top)                    kart.speed = top;
  if (kart.speed < -MAX_SPEED * REVERSE_FACTOR) kart.speed = -MAX_SPEED * REVERSE_FACTOR;

  const spd = Math.min(1, Math.abs(kart.speed) / MAX_SPEED);
  const sr  = STEER_AT_SPEED * (0.35 + 0.65 * spd) * kart.steerMul;
  const dh  = input.steer * sr * dt * (kart.speed >= 0 ? 1 : -1);
  kart.heading += dh;
  kart.angVel   = dt > 0 ? dh / dt : 0;

  kart.x += Math.cos(kart.heading) * kart.speed * dt;
  kart.y += Math.sin(kart.heading) * kart.speed * dt;

  const c = clampToTrack(kart.x, kart.y);
  kart.x = c.x; kart.y = c.y;
  if (c.hit) { kart.speed *= 0.55; kart.wallFlash = 0.25; }
  kart.wallFlash = Math.max(0, kart.wallFlash - dt);

  kart.trail.push({ x: kart.x, y: kart.y });
  if (kart.trail.length > TRAIL_LEN) kart.trail.shift();
}

function resolveOverlaps(karts) {
  for (let i = 0; i < karts.length; i++) {
    for (let j = i + 1; j < karts.length; j++) {
      const a = karts[i], b = karts[j];
      const dx = b.x - a.x, dy = b.y - a.y;
      const d  = Math.sqrt(dx * dx + dy * dy);
      const minD = KART_RADIUS * 2;
      if (d < minD && d > 0.001) {
        const push = (minD - d) * 0.5;
        const nx = dx / d, ny = dy / d;
        a.x -= nx * push; a.y -= ny * push;
        b.x += nx * push; b.y += ny * push;
        const ca = clampToTrack(a.x, a.y); a.x = ca.x; a.y = ca.y;
        const cb = clampToTrack(b.x, b.y); b.x = cb.x; b.y = cb.y;
      }
    }
  }
}

// ─── RENDER ──────────────────────────────────────────────────
function pathStadium(ctx, l, r, cy, rad) {
  ctx.moveTo(l, cy - rad);
  ctx.lineTo(r, cy - rad);
  ctx.arc(r, cy, rad, -Math.PI / 2, Math.PI / 2);
  ctx.lineTo(l, cy + rad);
  ctx.arc(l, cy, rad, Math.PI / 2, Math.PI * 1.5);
  ctx.closePath();
}

function drawTrack(ctx, opts = {}) {
  const fillColor  = opts.fillColor  || '#1a1a1f';
  const wallColor  = opts.wallColor  || '#333340';
  const wallWidth  = opts.wallWidth  || 4;

  ctx.save();
  ctx.fillStyle = fillColor;
  ctx.beginPath();
  pathStadium(ctx, STRAIGHT_LEFT, STRAIGHT_RIGHT, ARC_CY, OUTER_R);
  pathStadium(ctx, STRAIGHT_LEFT, STRAIGHT_RIGHT, ARC_CY, INNER_R);
  ctx.fill('evenodd');

  if (opts.showCenter !== false) {
    ctx.strokeStyle = 'rgba(255,255,255,0.05)';
    ctx.lineWidth = 2;
    ctx.setLineDash([14, 14]);
    ctx.beginPath();
    for (let i = 0; i <= NUM_WP; i++) {
      const p = WAYPOINTS[i % NUM_WP];
      i === 0 ? ctx.moveTo(p.x, p.y) : ctx.lineTo(p.x, p.y);
    }
    ctx.stroke();
    ctx.setLineDash([]);
  }

  ctx.strokeStyle = wallColor;
  ctx.lineWidth = wallWidth;
  ctx.beginPath(); pathStadium(ctx, STRAIGHT_LEFT, STRAIGHT_RIGHT, ARC_CY, OUTER_R); ctx.stroke();
  ctx.beginPath(); pathStadium(ctx, STRAIGHT_LEFT, STRAIGHT_RIGHT, ARC_CY, INNER_R); ctx.stroke();

  if (opts.showStartLine !== false) {
    ctx.fillStyle = 'rgba(255,255,255,0.4)';
    const x = STRAIGHT_LEFT + 60, cellH = (TRACK_HALF_W * 2) / 6;
    for (let i = 0; i < 6; i++) {
      const y = TOP_Y - TRACK_HALF_W + i * cellH;
      ctx.fillRect(x, y, 8, cellH - 2);
      if (i % 2 === 0) ctx.fillRect(x + 8, y, 8, cellH - 2);
    }
  }
  ctx.restore();
}

function drawKartBase(ctx, kart, bodyColor, outlineColor, outlineWidth) {
  bodyColor    = bodyColor    || kart.color;
  outlineColor = outlineColor || '#ffffff';
  outlineWidth = outlineWidth || 1.5;

  ctx.save();
  ctx.translate(kart.x, kart.y);
  ctx.rotate(kart.heading);
  ctx.fillStyle = 'rgba(0,0,0,0.45)';
  ctx.fillRect(-KART_LEN / 2 + 1, -KART_WID / 2 + 2, KART_LEN, KART_WID);
  ctx.fillStyle = kart.wallFlash > 0 ? '#ffffff' : bodyColor;
  ctx.fillRect(-KART_LEN / 2, -KART_WID / 2, KART_LEN, KART_WID);
  ctx.strokeStyle = outlineColor;
  ctx.lineWidth = outlineWidth;
  ctx.strokeRect(-KART_LEN / 2, -KART_WID / 2, KART_LEN, KART_WID);
  ctx.fillStyle = 'rgba(0,0,0,0.65)';
  ctx.beginPath();
  ctx.moveTo(KART_LEN / 2 - 5, -KART_WID / 3);
  ctx.lineTo(KART_LEN / 2 - 1, 0);
  ctx.lineTo(KART_LEN / 2 - 5,  KART_WID / 3);
  ctx.closePath();
  ctx.fill();
  ctx.restore();

  if (kart.isPlayer) {
    ctx.fillStyle = '#fff';
    ctx.beginPath();
    ctx.moveTo(kart.x,     kart.y - KART_LEN);
    ctx.lineTo(kart.x - 6, kart.y - KART_LEN - 9);
    ctx.lineTo(kart.x + 6, kart.y - KART_LEN - 9);
    ctx.closePath();
    ctx.fill();
  }
}

function drawTrail(ctx, kart, colorFn, width) {
  width = width || 2;
  for (let i = 1; i < kart.trail.length; i++) {
    const a = kart.trail[i - 1], b = kart.trail[i];
    const f = i / kart.trail.length;
    ctx.strokeStyle = colorFn ? colorFn(f, b) : `rgba(180,200,220,${f * 0.12})`;
    ctx.lineWidth = width;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.stroke();
  }
}

// ─── COMMON OVERLAY HELPERS ──────────────────────────────────
function fmtTime(sec) {
  const m = Math.floor(sec / 60), s = Math.floor(sec % 60);
  return `${m}:${String(s).padStart(2, '0')}`;
}

function radialGlow(ctx, x, y, r, colorStop0, colorStop1) {
  const g = ctx.createRadialGradient(x, y, 0, x, y, r);
  g.addColorStop(0, colorStop0);
  g.addColorStop(1, colorStop1 || 'transparent');
  ctx.fillStyle = g;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fill();
}

// ─── EXPORT ──────────────────────────────────────────────────
window.Engine = {
  W, H,
  STRAIGHT_LEFT, STRAIGHT_RIGHT, TOP_Y, BOTTOM_Y, ARC_CY,
  ARC_RADIUS, TRACK_HALF_W, INNER_R, OUTER_R,
  KART_LEN, KART_WID, KART_RADIUS,
  COLORS, PLAYER_COLOR,
  MAX_SPEED, ACCEL, BRAKE_DECEL, FRICTION_PER_S, STEER_AT_SPEED,
  REVERSE_FACTOR, TRAIL_LEN, NUM_WP, WAYPOINTS,
  clampToTrack, centerlineAt, nearestWaypointIndex, spawnPositionForIndex,
  Kart, keys, playerInput,
  aiInputToward, advanceWaypoint, nearestKart,
  stepKart, resolveOverlaps,
  pathStadium, drawTrack, drawKartBase, drawTrail,
  fmtTime, radialGlow,
};
})();
