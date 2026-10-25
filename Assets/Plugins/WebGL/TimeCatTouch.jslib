mergeInto(LibraryManager.library, {
 TimeCat_Pause: function(paused) { if(!paused && typeof WEBAudio !== "undefined" && WEBAudio.audioContext && WEBAudio.audioContext.state === "suspended") WEBAudio.audioContext.resume().catch(function(){}); requestAnimationFrame(function(){if(window.TimeCatPause)window.TimeCatPause(paused!==0);}); },
 TimeCat_Exit: function() { if(window.TimeCatExit) requestAnimationFrame(window.TimeCatExit); },
 TimeCat_Scene: function(name) { if(window.TimeCatScene) window.TimeCatScene(UTF8ToString(name)); },
 TimeCat_IsTouch: function() { return (/Android|iPhone|iPad|iPod/i.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && matchMedia('(pointer: coarse)').matches)) ? 1 : 0; }
});
