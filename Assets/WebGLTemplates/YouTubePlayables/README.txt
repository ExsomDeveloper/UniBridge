YouTube Playables — WebGL Template
===================================

Шаблон для сборки Unity WebGL под YouTube Playables.
Выбирается в Player Settings > WebGL > Resolution and Presentation.

Что делает шаблон:
  - Загружает YouTube Playables SDK (ytgame) до игрового кода
  - Вызывает ytgame.game.firstFrameReady() сразу после появления canvas
  - Загружает Unity WebGL билд через стандартный createUnityInstance()
  - Поддерживает portrait-режим с pillarbox (полосы по бокам)


Настраиваемые переменные
-------------------------

Находятся в начале <script> блока в index.html (строки 33-37).
Редактируйте их прямо в файле перед сборкой.

  PORTRAIT_MODE   (boolean, по умолчанию: false)
      true  — ограничить canvas портретным соотношением сторон,
              в landscape по бокам появятся полосы (pillarbox)
      false — canvas занимает весь viewport (responsive)

  PORTRAIT_RATIO  (number, по умолчанию: 9 / 16)
      Соотношение сторон: ширина / высота.
      Примеры:
        9 / 16   — стандартный portrait (телефон)
        3 / 4    — iPad-подобный portrait
        10 / 16  — чуть шире стандартного
        1 / 2    — очень узкий portrait

  PILLARBOX_COLOR (string, по умолчанию: '#000000')
      CSS-цвет полос по бокам canvas в landscape.
      Примеры:
        '#000000'          — чёрный
        '#1a1a2e'          — тёмно-синий
        '#2d2d2d'          — тёмно-серый
        'rgb(30, 0, 50)'   — тёмно-фиолетовый


Пример: включить portrait 9:16 с тёмно-синими полосами
------------------------------------------------------

  var PORTRAIT_MODE   = true;
  var PORTRAIT_RATIO  = 9 / 16;
  var PILLARBOX_COLOR = '#1a1a2e';


Требования YouTube Playables
-----------------------------

  - Начальный размер бандла: < 30 MiB (рекомендуется < 15 MiB)
  - Общий размер бандла: < 250 MiB
  - Размер сохранения: < 3 MiB
  - Имена файлов: только a-z, A-Z, 0-9, _, -, .
  - JS heap: < 512 MB
  - Игра ОБЯЗАНА поддерживать touch и mouse input
  - Игра НЕ ДОЛЖНА содержать внешних ссылок, IAP, внешней рекламы
