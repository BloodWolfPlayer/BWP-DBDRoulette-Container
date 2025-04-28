const { app, BrowserWindow, screen } = require('electron');
const WebSocket = require('ws'); // Import WebSocket

let windows = []; // Array to store multiple BrowserWindow instances
let Survivors = 1; // Number of survivors to reroll (min 0, max 4)
let Killer = false; // Whether to reroll killer perks
let ScreenSelection = 1; // Screen selection (0 for primary, 1 for secondary, etc.)
let CornerLoc = 0; // Corner location (0 for top-left, 1 for top-right, 2 for bottom-left, 3 for bottom-right)

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

  // Generate URLs for Survivor and Killer
  const survivorURLs = [
    "https://dpsm.3stadt.com/survivor?sids=0,1,3,72,83,4,65,5,107,68,6,7,8,9,81,11,66,128,95,99,12,13,14,133,16,17,80,18,20,21,98,143,144,141,69,92,114,22,23,108,131,104,70,24,25,26,27,28,29,130,64,145,30,31,32,33,35,113,37,38,39,115,111,40,41,123,67,42,96,148,43,44,77,140,47,48,50,135,51,52,53,147,54,55,56,59,60,134&streammode=1",
    "https://dpsm.3stadt.com/survivor?sids=4,65,5,107,68&streammode=1",
    "https://dpsm.3stadt.com/survivor?sids=6,7,8,9,81&streammode=1",
    "https://dpsm.3stadt.com/survivor?sids=11,66,128,95,99&streammode=1",
  ];
  const killerURL = "https://dpsm.3stadt.com/killer?streammode=1";




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
    win.loadURL(survivorURLs[i]); // Load the corresponding Survivor URL
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
    win.loadURL(killerURL); // Load the Killer URL
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