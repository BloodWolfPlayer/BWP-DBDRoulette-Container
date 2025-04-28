const { app, BrowserWindow, screen } = require('electron');
const WebSocket = require('ws'); // Import WebSocket

let windows = []; // Array to store multiple BrowserWindow instances



const args = process.argv.slice(2); // Skip the first two arguments (node and script path)
let Survivors = parseInt(args[0]) || 1; // Default to 1 if not provided
let Killer = args[1] === 'true'; // Convert string to boolean
let ScreenSelection = parseInt(args[2]) || 0; // Default to 0 if not provided
let CornerLoc = parseInt(args[3]) || 0; // Default to 0 if not provided
const SurvivorURLs = [
  args[4] || "https://dpsm.3stadt.com/survivor?streammode=1", // Default to all perks
  args[5] || "https://dpsm.3stadt.com/survivor?streammode=1", 
  args[6] || "https://dpsm.3stadt.com/survivor?streammode=1", 
  args[7] || "https://dpsm.3stadt.com/survivor?streammode=1", 
];
let KillerURL = args[8] || "https://dpsm.3stadt.com/killer?streammode=1"; // Default to all perks

// add "+"&streammode=1" to the end of the URLs, if not given by user in settings.
for (let i = 0; i < SurvivorURLs.length; i++) {
  if (!SurvivorURLs[i].includes("&streammode=1") && !SurvivorURLs[i].includes("?streammode=1")) {
    if (SurvivorURLs[i].includes("?")) {
    SurvivorURLs[i] += "&streammode=1";
  }
}
}

if (!KillerURL.includes("&streammode=1") && !KillerURL.includes("?streammode=1")) {
  KillerURL += "&streammode=1";
}

if (Survivors == 0 && !Killer){
  console.error("Invalid configuration: At least one window (Survivor or Killer) must be created.");
  process.exit(2);
}

// Create WebSocket server
const wss = new WebSocket.Server({ port: 8080 });

wss.on('connection', (ws) => {
  console.log('WebSocket connection established.');

  ws.on('message', (message) => {
    console.log(`Received message: ${message}`);

    if (message == 'reroll') {
      console.log('Reroll command received. Triggering rerollPerks.');
      rerollPerks();
    } else {
      console.log('Unknown command received.');
    }
  });

  ws.on('close', () => {
    console.log('WebSocket connection closed.');
  });
});

function createWindows() {
  const displays = screen.getAllDisplays();
  const selectedDisplay = displays[ScreenSelection] || displays[0]; // Default to primary display if out of bounds
  let baseX = selectedDisplay.bounds.x; // Base X position
  let baseY = selectedDisplay.bounds.y; // Base Y position

  // Clear any existing windows
  windows.forEach((win) => win.close());
  windows = [];
  switch (CornerLoc) {
    case 0: // Top-left corner
      // baseX and baseY are already set to the top-left of the selected display
      break;
    case 1: // Top-right corner
      baseX += selectedDisplay.bounds.width - 800; // Subtract window width
      break;
    case 2: // Bottom-left corner
      baseY += selectedDisplay.bounds.height - 200; // Subtract window height
      break;
    case 3: // Bottom-right corner
      baseX += selectedDisplay.bounds.width - 800; // Subtract window width
      baseY += selectedDisplay.bounds.height - 200; // Subtract window height
      break;
    default:
      console.warn("Invalid CornerLoc value. Defaulting to top-left.");
      break;
  }

  // Create windows for Survivors
  for (let i = 0; i < Survivors; i++) {
    const windowOptions = {
      width: 800,
      height: 200,
      x: baseX,
      y: CornerLoc === 0 || CornerLoc === 1 ? baseY + i * 200 : baseY - i * 200, // Stack downward for 0/1, upward for 2/3
      frame: false,
      transparent: true,
      alwaysOnTop: true,
      hasShadow: false,
      resizable: false,
      backgroundColor: '#00000000', // Fully transparent background
    };

    const win = new BrowserWindow(windowOptions);
    win.loadURL(SurvivorURLs[i]); // Load the corresponding Survivor URL
    setupWindow(win);
    windows.push(win);
  }

  // Create a window for Killer if enabled
  if (Killer || Survivors === 0) {
    const windowOptions = {
      width: 800,
      height: 200,
      x: baseX,
      y: CornerLoc === 0 || CornerLoc === 1 ? baseY + Survivors * 200 : baseY - Survivors * 200, // Stack downward for 0/1, upward for 2/3
      frame: false,
      transparent: true,
      alwaysOnTop: true,
      hasShadow: false,
      resizable: false,
      backgroundColor: '#00000000', // Fully transparent background
    };

    const win = new BrowserWindow(windowOptions);
    win.loadURL(KillerURL); // Load the Killer URL
    setupWindow(win);
    windows.push(win);
  }
}






function setupWindow(win) {
  win.webContents.on('did-finish-load', () => {
    win.webContents.executeJavaScript(`
      document.body.style.background = 'transparent';
      document.documentElement.style.background = 'transparent';
      document.body.style.margin = '0';
      document.body.style.overflow = 'hidden';
      document.body.style.border = 'none'; // Ensure no border
      document.documentElement.style.border = 'none'; // Ensure no border
    `);
  });
}

function rerollPerks() {
  if (windows.length === 0) {
    console.error('No windows are initialized.');
    return;
  }

  windows.forEach((win) => {
    const { webContents } = win;

    // Attach debugger if not already attached
    if (!webContents.debugger.isAttached()) {
      try {
        webContents.debugger.attach('1.3');
        console.log('[reroll] Debugger attached.');
      } catch (err) {
        console.error('Debugger attach failed:', err);
        return;
      }
    }

    // Get the bounding box of the .slot element
    webContents.executeJavaScript(`
      (function() {
        const el = document.querySelector('.slot');
        if (!el) return null;
        const rect = el.getBoundingClientRect();
        return {
          x: rect.left + rect.width / 2,
          y: rect.top + rect.height / 2
        };
      })();
    `).then(async (coords) => {
      if (!coords) {
        console.warn('[reroll] .slot element not found in DOM.');
        return;
      }

      const x = Math.floor(coords.x);
      const y = Math.floor(coords.y);

      try {
        // Send mousePressed event
        await webContents.debugger.sendCommand('Input.dispatchMouseEvent', {
          type: 'mousePressed',
          x,
          y,
          button: 'left',
          clickCount: 1,
        });

        // Send mouseReleased event
        await webContents.debugger.sendCommand('Input.dispatchMouseEvent', {
          type: 'mouseReleased',
          x,
          y,
          button: 'left',
          clickCount: 1,
        });

        console.log('[reroll] Native mouse click sent.');
      } catch (err) {
        console.error('Error dispatching mouse event:', err);
      }
    }).catch((err) => {
      console.error('Error evaluating element position:', err);
    });
  });
}

app.whenReady().then(() => {
  createWindows();

  app.on('activate', function () {
    if (BrowserWindow.getAllWindows().length === 0) createWindows();
  });
});

app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') app.quit();
});

module.exports = { createWindows, rerollPerks };
