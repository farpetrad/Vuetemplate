﻿import 'core-js/stable';
import 'regenerator-runtime/runtime';

import { FontAwesomeIcon, FontAwesomeLayers, FontAwesomeLayersText } from '@fortawesome/vue-fontawesome';

import { library, config } from '@fortawesome/fontawesome-svg-core';
import {
  faTimes,
  faEllipsisVAlt,
} from '@fortawesome/pro-regular-svg-icons';

config.autoAddCss = false;


import Modal from 'components/modal/Modal';
import Drawer from 'components/Drawer';
import Navbar from 'components/nav/Navbar';


import Vue from 'vue';
import App from './App.vue';
import router from './router';
import store from './store';



Vue.component('modal', Modal);
Vue.component(Drawer.name, Drawer);
Vue.component(Navbar.name, Navbar);

Vue.component('font-awesome-icon', FontAwesomeIcon);
Vue.component('font-awesome-layers', FontAwesomeLayers);
Vue.component('font-awesome-layers-text', FontAwesomeLayersText);

library.add(faTimes);
library.add(faEllipsisVAlt);

new Vue({
  el: '#app',
  router,
  store,
  render: (h) => h(App),
}).$mount('#app');
