(() => {
  const COLS = 10;
  const ROWS = 20;
  const CELL = 30;

  const SHAPES = {
    I: [
      [[0, 1], [1, 1], [2, 1], [3, 1]],
      [[2, 0], [2, 1], [2, 2], [2, 3]],
      [[0, 2], [1, 2], [2, 2], [3, 2]],
      [[1, 0], [1, 1], [1, 2], [1, 3]],
    ],
    O: [
      [[1, 0], [2, 0], [1, 1], [2, 1]],
      [[1, 0], [2, 0], [1, 1], [2, 1]],
      [[1, 0], [2, 0], [1, 1], [2, 1]],
      [[1, 0], [2, 0], [1, 1], [2, 1]],
    ],
    T: [
      [[1, 0], [0, 1], [1, 1], [2, 1]],
      [[1, 0], [1, 1], [2, 1], [1, 2]],
      [[0, 1], [1, 1], [2, 1], [1, 2]],
      [[1, 0], [0, 1], [1, 1], [1, 2]],
    ],
    S: [
      [[1, 0], [2, 0], [0, 1], [1, 1]],
      [[1, 0], [1, 1], [2, 1], [2, 2]],
      [[1, 1], [2, 1], [0, 2], [1, 2]],
      [[0, 0], [0, 1], [1, 1], [1, 2]],
    ],
    Z: [
      [[0, 0], [1, 0], [1, 1], [2, 1]],
      [[2, 0], [1, 1], [2, 1], [1, 2]],
      [[0, 1], [1, 1], [1, 2], [2, 2]],
      [[1, 0], [0, 1], [1, 1], [0, 2]],
    ],
    J: [
      [[0, 0], [0, 1], [1, 1], [2, 1]],
      [[1, 0], [2, 0], [1, 1], [1, 2]],
      [[0, 1], [1, 1], [2, 1], [2, 2]],
      [[1, 0], [1, 1], [0, 2], [1, 2]],
    ],
    L: [
      [[2, 0], [0, 1], [1, 1], [2, 1]],
      [[1, 0], [1, 1], [1, 2], [2, 2]],
      [[0, 1], [1, 1], [2, 1], [0, 2]],
      [[0, 0], [1, 0], [1, 1], [1, 2]],
    ],
  };

  const COLORS = {
    I: "#33d6f2",
    O: "#fadb38",
    T: "#ba54f5",
    S: "#52db61",
    Z: "#f0474c",
    J: "#4572f5",
    L: "#fa942e",
  };

  const TYPES = Object.keys(SHAPES);
  const KICKS = [[0, 0], [-1, 0], [1, 0], [0, 1], [-1, 1], [1, 1], [-2, 0], [2, 0], [0, -1]];
  const GRAVITY = [48, 43, 38, 33, 28, 23, 18, 13, 8, 6, 5, 5, 4, 4, 3, 3, 2, 2, 1];
  const LINE_SCORE = [0, 100, 300, 500, 800];

  const boardCanvas = document.getElementById("board");
  const nextCanvas = document.getElementById("next");
  const overlay = document.getElementById("overlay");
  const overlayTitle = document.getElementById("overlay-title");
  const overlayHint = document.getElementById("overlay-hint");
  const restartButton = document.getElementById("restart");
  const scoreEl = document.getElementById("score");
  const levelEl = document.getElementById("level");
  const linesEl = document.getElementById("lines");
  const ctx = boardCanvas.getContext("2d");
  const nextCtx = nextCanvas.getContext("2d");

  const state = {
    board: createBoard(),
    active: null,
    next: null,
    bag: [],
    score: 0,
    level: 1,
    lines: 0,
    status: "playing",
    gravityMs: 0,
    dasDir: 0,
    dasTime: 0,
    flashRows: null,
    flashLeft: 0,
  };

  function createBoard() {
    return Array.from({ length: ROWS }, () => Array(COLS).fill(null));
  }

  function cellsOf(piece) {
    return SHAPES[piece.type][piece.rotation].map(([x, y]) => [x + piece.x, y + piece.y]);
  }

  function fits(piece) {
    return cellsOf(piece).every(([x, y]) => x >= 0 && x < COLS && y >= 0 && y < ROWS && !state.board[y][x]);
  }

  function refillBag() {
    const bag = TYPES.slice();
    for (let i = bag.length - 1; i > 0; i -= 1) {
      const j = Math.floor(Math.random() * (i + 1));
      [bag[i], bag[j]] = [bag[j], bag[i]];
    }
    state.bag.push(...bag);
  }

  function takePiece() {
    if (!state.bag.length) refillBag();
    return { type: state.bag.shift(), x: 3, y: 0, rotation: 0 };
  }

  function spawn() {
    state.active = state.next || takePiece();
    state.active.x = 3;
    state.active.y = 0;
    state.active.rotation = 0;
    state.next = takePiece();
    state.gravityMs = 0;
    if (!fits(state.active)) state.status = "over";
  }

  function gravityInterval() {
    return (GRAVITY[Math.min(state.level - 1, GRAVITY.length - 1)] / 60) * 1000;
  }

  function tryMove(dx, dy) {
    const moved = { ...state.active, x: state.active.x + dx, y: state.active.y + dy };
    if (!fits(moved)) return false;
    state.active = moved;
    return true;
  }

  function tryRotate(dir) {
    for (const [kx, ky] of KICKS) {
      const rotated = {
        ...state.active,
        rotation: (state.active.rotation + dir + 4) % 4,
        x: state.active.x + kx,
        y: state.active.y + ky,
      };
      if (fits(rotated)) {
        state.active = rotated;
        return;
      }
    }
  }

  function ghostPiece() {
    const ghost = { ...state.active };
    while (fits({ ...ghost, y: ghost.y + 1 })) ghost.y += 1;
    return ghost;
  }

  function hardDrop() {
    let dropped = 0;
    while (tryMove(0, 1)) dropped += 1;
    state.score += dropped * 2;
    lockPiece();
  }

  function fullRows() {
    const rows = [];
    for (let y = 0; y < ROWS; y += 1) {
      if (state.board[y].every(Boolean)) rows.push(y);
    }
    return rows;
  }

  function lockPiece() {
    for (const [x, y] of cellsOf(state.active)) state.board[y][x] = state.active.type;
    const rows = fullRows();
    if (rows.length) {
      state.status = "clearing";
      state.flashRows = new Set(rows);
      state.flashLeft = 180;
      return;
    }
    spawn();
  }

  function finishClear() {
    const cleared = fullRows().length;
    state.board = state.board.filter((row) => !row.every(Boolean));
    while (state.board.length < ROWS) state.board.unshift(Array(COLS).fill(null));
    state.lines += cleared;
    state.score += LINE_SCORE[cleared] * state.level;
    state.level = 1 + Math.floor(state.lines / 10);
    state.flashRows = null;
    state.status = "playing";
    spawn();
  }

  function restart() {
    Object.assign(state, {
      board: createBoard(),
      bag: [],
      score: 0,
      level: 1,
      lines: 0,
      status: "playing",
      gravityMs: 0,
      dasDir: 0,
      dasTime: 0,
      flashRows: null,
    });
    state.next = takePiece();
    spawn();
  }

  function drawCell(context, x, y, color, alpha = 1, size = CELL) {
    context.globalAlpha = alpha;
    context.fillStyle = color;
    context.fillRect(x * size + 1, y * size + 1, size - 2, size - 2);
    context.globalAlpha = 1;
  }

  function drawBoard() {
    ctx.clearRect(0, 0, boardCanvas.width, boardCanvas.height);
    for (let y = 0; y < ROWS; y += 1) {
      for (let x = 0; x < COLS; x += 1) {
        ctx.fillStyle = "#161827";
        ctx.fillRect(x * CELL, y * CELL, CELL, CELL);
        if (state.status === "clearing" && state.flashRows.has(y)) {
          drawCell(ctx, x, y, "#ffffff");
        } else if (state.board[y][x]) {
          drawCell(ctx, x, y, COLORS[state.board[y][x]]);
        }
      }
    }

    if (state.status === "playing" || state.status === "paused") {
      const ghost = ghostPiece();
      if (ghost.y !== state.active.y) {
        for (const [x, y] of cellsOf(ghost)) drawCell(ctx, x, y, COLORS[state.active.type], 0.28);
      }
      for (const [x, y] of cellsOf(state.active)) drawCell(ctx, x, y, COLORS[state.active.type]);
    }
  }

  function drawNext() {
    nextCtx.clearRect(0, 0, nextCanvas.width, nextCanvas.height);
    const cells = SHAPES[state.next.type][0];
    const size = 22;
    const minX = Math.min(...cells.map(([x]) => x));
    const maxX = Math.max(...cells.map(([x]) => x));
    const minY = Math.min(...cells.map(([, y]) => y));
    const maxY = Math.max(...cells.map(([, y]) => y));
    const ox = Math.floor((nextCanvas.width - (maxX - minX + 1) * size) / 2);
    const oy = Math.floor((nextCanvas.height - (maxY - minY + 1) * size) / 2);
    nextCtx.fillStyle = COLORS[state.next.type];
    for (const [x, y] of cells) {
      nextCtx.fillRect(ox + (x - minX) * size + 1, oy + (y - minY) * size + 1, size - 2, size - 2);
    }
  }

  function renderHud() {
    scoreEl.textContent = String(state.score).padStart(6, "0");
    levelEl.textContent = String(state.level);
    linesEl.textContent = String(state.lines);
    overlay.classList.toggle("hidden", state.status !== "paused" && state.status !== "over");
    restartButton.hidden = state.status !== "over";
    if (state.status === "over") {
      overlayTitle.textContent = "ИГРА ОКОНЧЕНА";
      overlayHint.textContent = `Счёт: ${state.score}`;
    } else if (state.status === "paused") {
      overlayTitle.textContent = "ПАУЗА";
      overlayHint.textContent = "P — продолжить";
    }
  }

  function render() {
    drawBoard();
    drawNext();
    renderHud();
  }

  let last = performance.now();
  function loop(now) {
    const dt = now - last;
    last = now;
    if (state.status === "clearing") {
      state.flashLeft -= dt;
      if (state.flashLeft <= 0) finishClear();
    } else if (state.status === "playing") {
      const soft = keys.has("ArrowDown") || keys.has("s");
      state.gravityMs += dt;
      if (state.gravityMs >= (soft ? 40 : gravityInterval())) {
        state.gravityMs = 0;
        if (!tryMove(0, 1)) lockPiece();
        else if (soft) state.score += 1;
      }
      handleDas(dt);
    }
    render();
    requestAnimationFrame(loop);
  }

  const keys = new Set();
  function handleDas(dt) {
    let dir = 0;
    if (keys.has("ArrowLeft") || keys.has("a")) dir -= 1;
    if (keys.has("ArrowRight") || keys.has("d")) dir += 1;
    if (!dir) {
      state.dasDir = 0;
      state.dasTime = 0;
      return;
    }
    if (dir !== state.dasDir) {
      state.dasDir = dir;
      state.dasTime = 0;
      tryMove(dir, 0);
      return;
    }
    state.dasTime += dt;
    if (state.dasTime >= 170 && Math.floor((state.dasTime - 170) / 45) !== Math.floor((state.dasTime - 170 - dt) / 45)) {
      tryMove(dir, 0);
    }
  }

  window.addEventListener("keydown", (event) => {
    const raw = event.key;
    if (["ArrowLeft", "ArrowRight", "ArrowDown", "ArrowUp", " "].includes(raw)) event.preventDefault();
    const key = raw.length === 1 ? raw.toLowerCase() : raw;
    keys.add(key);

    if (state.status === "over") {
      if (key === "r" || key === "Enter") restart();
      return;
    }
    if (key === "p" || key === "Escape") {
      state.status = state.status === "paused" ? "playing" : "paused";
      return;
    }
    if (state.status !== "playing") return;
    if (key === "ArrowUp" || key === "x" || key === "w") tryRotate(1);
    if (key === "z" || key === "Control") tryRotate(-1);
    if (raw === " ") hardDrop();
    if (key === "r") restart();
  });

  window.addEventListener("keyup", (event) => {
    keys.delete(event.key.length === 1 ? event.key.toLowerCase() : event.key);
  });

  restartButton.addEventListener("click", restart);
  restart();
  requestAnimationFrame(loop);
})();
