var getAddress = (function (exports) {
    'use strict';

    var _a$2;class Storage{}_a$2=Storage,Storage['key']='getaddress_local_addresses',Storage['save']=(_0xc20ae8,_0x52502f)=>{let _0x4b1599=localStorage['getItem'](_a$2['key']);if(!_0x4b1599||_a$2['isObjectEmpty'](_0x4b1599)){const _0x4d96ad=new Map(),_0x1219af=new StoredAddress(_0xc20ae8,_0x52502f,new Date()['getTime']());_0x4d96ad['set'](_0xc20ae8['id'],_0x1219af);const _0xa5833c=JSON['stringify']([..._0x4d96ad]);localStorage['setItem'](_a$2['key'],_0xa5833c);}else {const _0xadd7bd=new Map(JSON['parse'](_0x4b1599));if(!_0xadd7bd['get'](_0xc20ae8['id'])){if(_0xadd7bd['size']>=0x6){const _0x5aeafb=[..._0xadd7bd['entries']()]['sort']((_0x3e60ec,_0x48f59b)=>{const _0x30b4b1=_0x3e60ec[0x1],_0x108184=_0x48f59b[0x1];return _0x30b4b1['timestamp']-_0x108184['timestamp'];});_0xadd7bd['delete'](_0x5aeafb[0x0][0x0]);}const _0x518916=new StoredAddress(_0xc20ae8,_0x52502f,new Date()['getTime']());_0xadd7bd['set'](_0xc20ae8['id'],_0x518916);const _0x54827b=JSON['stringify']([..._0xadd7bd]);localStorage['setItem'](_a$2['key'],_0x54827b);}}},Storage['list']=()=>{let _0x2d6a4d=localStorage['getItem'](_a$2['key']);if(!_0x2d6a4d||_a$2['isObjectEmpty'](_0x2d6a4d))return [];const _0x5ed408=new Map(JSON['parse'](_0x2d6a4d)),_0x4b7f75=[..._0x5ed408['entries']()]['map'](_0x4cb1fb=>{const _0x20a4d9=_0x4cb1fb[0x1];return _0x20a4d9;});return _0x4b7f75['reverse']();},Storage['isObjectEmpty']=_0x304bdb=>{if(!_0x304bdb)return !![];return _0x304bdb==='{}'||_0x304bdb==='\x22{}\x22';},Storage['clear']=()=>{localStorage['removeItem'](_a$2['key']);},Storage['clearExpired']=()=>{let _0xf7074=localStorage['getItem'](_a$2['key']);if(!_0xf7074||_a$2['isObjectEmpty'](_0xf7074))return;const _0x5461a0=new Map(JSON['parse'](_0xf7074));let _0x48224c=![];_0x5461a0['forEach']((_0x9dde04,_0x144064)=>{const _0x1db46a=_0x9dde04;let _0x317c34=new Date();var _0x28c07f=new Date(new Date()['setDate'](_0x317c34['getDate']()-0x7));_0x28c07f['getTime']()>_0x1db46a['timestamp']&&(_0x5461a0['delete'](_0x144064),_0x48224c=!![]);});if(_0x48224c){if(_0x5461a0['size']>0x0){const _0x5001c5=JSON['stringify']([..._0x5461a0]);localStorage['setItem'](_a$2['key'],_0x5001c5);}else localStorage['removeItem'](_a$2['key']);}},Storage['remove']=_0x573752=>{let _0x51a7fb=localStorage['getItem'](_a$2['key']);if(!_0x51a7fb||_a$2['isObjectEmpty'](_0x51a7fb))return;const _0x54e5be=new Map(JSON['parse'](_0x51a7fb));let _0x1dc7be=![];_0x54e5be['forEach']((_0x25d52c,_0x5e0dd2)=>{_0x5e0dd2===_0x573752&&(_0x54e5be['delete'](_0x573752),_0x1dc7be=!![]);});if(_0x1dc7be){if(_0x54e5be['size']>0x0){const _0x395a5b=JSON['stringify']([..._0x54e5be]);localStorage['setItem'](_a$2['key'],_0x395a5b);}else localStorage['removeItem'](_a$2['key']);}};class StoredAddress{constructor(_0x4e83d6,_0x2fbd4b,_0x436262){this['suggestion']=_0x4e83d6,this['address']=_0x2fbd4b,this['timestamp']=_0x436262;}}

    class List{constructor(_0xfb84fb,_0x37e105){this['options']=_0xfb84fb,this['instance']=_0x37e105,this['list']=document['createElement']('DIV'),this['ul']=document['createElement']('DIV'),this['style']=document['createElement']('style'),this['element']=this['list'],this['selectedIndex']=-0x1,this['handleKeyDown']=_0x1580fd=>{switch(_0x1580fd['key']){case'PageUp':this['handlePageUp'](_0x1580fd);break;case'PageDown':this['handlePageDown'](_0x1580fd);break;}},this['handlePageDown']=_0x574b76=>{this['ul']['children']['length']>0x0&&(this['setSelectedIndex'](this['lastIndex']()),_0x574b76['preventDefault']());},this['handlePageUp']=_0x56f02f=>{this['ul']['children']['length']>0x0&&(this['setSelectedIndex'](0x0),_0x56f02f['preventDefault']());},this['populate']=_0x52fe19=>{this['clear']();_0x52fe19['length']>0x0&&this['show']();_0x52fe19['forEach'](_0x345ea4=>{this['ul']['insertBefore'](_0x345ea4,null);});if(this['options']['footer_template']){var _0x2d0057=document['createElement']('DIV');_0x2d0057['innerHTML']=this['options']['footer_template'](_0x52fe19),this['list']['insertAdjacentElement']('beforeend',_0x2d0057);}},this['show']=()=>{this['list']['style']['visibility']='visible';},this['removeFocusedClassName']=()=>{List['removeFocusedClassName'](this);},this['clear']=()=>{this['ul']['replaceChildren'](...[]),this['hide'](),this['resetSelectedIndex']();},this['hide']=()=>{this['list']['style']['visibility']='hidden';},this['listItemCss']=_0x43915a=>{if(_0x43915a!==0x0)return '';const _0x5ad073='\x0a\x20\x20\x20\x20\x20\x20\x20\x20/*\x20List\x20Item\x20*/\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-list-item:hover\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20background-color:var(--ga-autocomplete-list-item-background-hover-color,rgba(10,\x2010,\x2010,\x20.04));\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20cursor:\x20var(--ga-autocomplete-list-item-hover-cursor,pointer);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20outline:var(--ga-autocomplete-list-item-hover-outline,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-list-item-focused\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20background-color:var(--ga-autocomplete-list-item-background-focused-color,rgba(10,\x2010,\x2010,\x20.12));\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20outline:var(--ga-autocomplete-list-item-focused-outline,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20}';return _0x5ad073;},this['iconCss']=_0x12a5ca=>{if(_0x12a5ca!==0x0)return '';const _0x2717bd='\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20/*icons*/\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20@font-face\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-family:\x20\x27getAddress\x27;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20src:\x20url(\x27data:application/octet-stream;base64,d09GRgABAAAAAA6QAA8AAAAAGQAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAABHU1VCAAABWAAAADsAAABUIIslek9TLzIAAAGUAAAARAAAAGA+I1IfY21hcAAAAdgAAABnAAABsOGn9KljdnQgAAACQAAAAAsAAAAOAAAAAGZwZ20AAAJMAAAG7QAADgxiLvl6Z2FzcAAACTwAAAAIAAAACAAAABBnbHlmAAAJRAAAAn8AAAMYOWZZRGhlYWQAAAvEAAAAMQAAADYrAaynaGhlYQAAC/gAAAAcAAAAJAfdA/pobXR4AAAMFAAAABgAAAAYFRQAAGxvY2EAAAwsAAAADgAAAA4ClgG4bWF4cAAADDwAAAAgAAAAIAEYDnFuYW1lAAAMXAAAAXQAAALNzZ0cHXBvc3QAAA3QAAAAQQAAAFMSDgC1cHJlcAAADhQAAAB6AAAAnH62O7Z4nGNgZGBg4GIwYLBjYHJx8wlh4MtJLMljkGJgYYAAkDwymzEnMz2RgQPGA8qxgGkOIGaDiAIAJjsFSAB4nGNgYW5mnMDAysDAVMW0h4GBoQdCMz5gMGRkAooysDIzYAUBaa4pDAdeMHwwZA76n8UQxbyGYRpQmBFFERMAfgwMyHic7ZHBEYAgDAQ3Aj4cSvGDdViDL4vP0w70ApThzSw3OQYeF6AASewig90YoUup9Tyx9TxzaF7lxuLpae8LznDJdFe7L3qT42db+VX7ec6pRGeD6Ncn6gyfxD58Ejt52oDyAf4/FBYAeJxjYEAGAAAOAAEAeJytV2tbG8cVntUNjAEDQtjNuu4oY1GXHckkcRxiKw7ZZVEcJanAuN11brtIuE2TXpLe6DW9X5Q/c1a0T51v+Wl5z8xKAQfcp89TPui8M/POnOucWUhoSeJ+FMZSdh+J+Z0uVe49iOiGS9fi5KEc3o+o0Eg/mxbTot9X+269TiImEaitkXBEkPhNcjTJ5GGTClrVVb1JRS0HR8XlmvADqgYySfyssBz4WaMYUCHYO5Q0qwCCdECl3uGoUCjgGKofXK7z7Gi+5viXJaDyR1WnijVFohcdxKMVp2AUljQVPaoFEeujlSDICa4cSPq8R6XVB6NrzlwQ9kOqhFGdio14960IZHcYSer1MLUJNm0w2ohjmVk2LLqGqXwkaZ3X15n5eS+SiMYwlTTTixLMSF6bYXST0c3ETeI4dhEtmg36JHYjEl0m1zF2u3SF0ZVu+mhB9JnxqCz243iQxuR4cZx7EMsB/FF+3KSylrCg1Ejh01TQi2hK+TStfGQAW5ImVUy4EQk5yKb2fcmL7K5rzedfEknYp/JaHYuBHMohdGXr5QYitBMlPTfdjSMV12NJm/cirLkcl9yUJk1pOhd4I1GwaZ7GUPkK5aL8lAr7D8npwxCaWmvSOS3Z2nm4VRL7kk+gzSRmSrJlrJ3Ro3PzIgj9tfqkcM7rk4U0a09xPJgQwPVEhkOVclJNsIXLCSHpwsixlUitSresirkzttNV7BLul64d3zSvjUNHc7OiGEKLq+rxGor4gs4KhZAG6VaTFjSoUtKF4DU+AAAZogUe7WK0YPK1iIMWTFAkYtCHZloMEjlMJC0ibE1a0t29KCsNtuKrNHegDptU1d2dqHvPTrp1zFfN/LLOxFJwP8qWlgJyUp8WPb5yKC0/u8A/C/ghZwW5KDZ6Ucbhg7/+EBmG2oW1usK2MXbtOm/BTeaZGJ50YH8HsyeTdUYKMyGqCvFCQd0ZOY5jslXTIhOFcC+iJeXLkOZRfnOIcOLL5D+XLjliUVSF7/scgWWsOWm2PO3Rp577NMK1Ah9rXpMu6sxheQnxZvk1nRVZPqWzEktXZ2WWl3VWYfl1nU2xvKKzaZbf0Nk5lp5W4/hTJUGklWyR8w7flibpY4srk8WP7GLz2OLqZPFjuyi1oAvemX7CqX9bV9nP4/7V4Z+EXU/DP5YK/rG8Cv9YNuAfy1X4x/Kb8I/lNfjH8lvwj+Ua/GPZ0rJtCva6htpLiUTTc5LApBSXsMU1u67pukfXcR+fwVXoyDOyqdINxY39iQyXvX92nOJsvhJyxdEza1nZqYURmiJ7+dyx8JzFuaHl88by53Ga5YRf1Ylre6otPC9W/iX4b+uO2shuODX29SbiAQdOtx+XJd1o0gu6dbHdpI3/RkVh90F/ESkSKw3Zkh1uCQjt3eGwozroIREePnRdvEgbjlNbRoRvoXet0EXQSminDUPLZoVP5wPvYNhSUraHOPP2SZps2fOoovwxW1LCPWVzJzoqybJ0j0qr5adinzvtDJq2MjvUdkKV4PHrmnC3s69SKUgGisp4VLFcClIXOOFO9/ieFKah/6tt5FhBwza/WDOB0YLzTlGibE+toIkgGWUUXPkrp+JENqLBRhTxm3fSL3WhENrjWEjMllfzWKg2wvTSZIlmzPq26rBSzuKdSQjZGRtpEntRS7bxoLP1+aRku/JUUKWB0d3j3y42iadVe54txSX/8jFLgnG6Ev7AedzlcYo30T9aHMVtuhhEPRdvqmzHrWzdWca9feXE6q7bO7Hqn7r3STsCTbe8Jync0nTbG8I2rjE4dSYVCW3ROnaExmWuz1Ub+RQfaL51nQtU4fq0cPPs+ds6m8FbM97yP5Z05/9VxewT97G2Qqs6Vi/1OLezgwZ8yxtH5VWMbnt1lccl92YSgrsIQc1ee3yN4IZXW3QTt/y1M+a7OM5ZrtILwK9rehHiDY5iiHDLbTy842i9qbmg6Q3Ab+uRENsAPQCHwY4eOWZmF8DM3GNOB2CPOQzuM4fBd5jD4Lv6CL0wAIqAHINifeTYuQdAdu4t5jmM3maeQe8wz6B3mWfQe6wzBEhYJ4OUdTLYZ50M+sx5FWDAHAYHzGHwkDkMvmfs2gL6vrGL0fvGLkY/MHYx+sDYxehDYxejHxq7GP3I2MXox4hxe5LAn5gRbQJ+ZOErgB9z0M3Ix+ineGtzzs8sZM7PDcfJOb/A5pcmp/7SjMyOQwt5x68sZPqvcU5O+I2FTPithUz4Hbh3Juf93owM/RMLmf4HC5n+R+zMCX+ykAl/tpAJfwH35cl5fzUjQ/+bhUz/u4VM/wd25oR/WsiEoYVM+FSPzpsvW6q4o1KhGOKfJrTB2Pdo+oCKV3uH48e6+QUl2gFBAAAAAAEAAf//AA94nI1STUgUYRh+3++b/XZmzXX+dmZnNtTdmWZDCelzf07pnBZjF2xPph3Uk4SdgupSIqFGEAQW0q1Th4T+L5287EIdFMIk8F63Tq4QlE59sxJ07P1+eV5enufhfQFBBHlJ3sJp6A9zWS1FJQLYQAQCqxQJuWlmTVti7nC5NI7FoFThA1itcMvsQ9syWWEEk6wQuK2WW6q57ZYzwZ1Wy+ETTqvtTIzO+G78cqfdjrF2yz3Jjwpawf37A31OfoIJfpiPgVWghK6BoMUpQCSzQJBMmp6vxwo0POHNi5t5eUGfxiKuGdzXO+Su4/vO8VJH97mB83hP90tax+d+Ry8ViBHbpIJvnT6hQyCDBjZcDGtWxjQkImEjiZJglshyQqiiEtCrDIkQMM1iLfNCETZ1PaUg6LZup3sVLaUlJJBRlpk1jBxUGBTH4BU1GGSqZWTsZFHD7S1MRwfRSnSA6a2N3d3oy97e+w2+SYf+ongb00fbezjUTZJrhyKLOkhC6yfpPvkOSegDQ6h9VH/dc+lyWBY26CowWZHZdSLEogS4AFIKZSbJC709p6iSSChT3Y+SmE1iQklM5sLKP3XK8n8XToeuaaqqaZu2lVEN1dC1k1DTbGAY85l8WSzMa3nN07wzZU8AXobeQPPXLeJj8+gHfXX8cXOHPF3H5joZn5l5Ee1ENp6LPh++EdEdP+H1MXlHuXA5CEHoAaFICS6LCVgR3aAwHXdvPnbQNLKu1Z2FUrVSHUPeH09hkiUpKxRpUAyqpsUrpaDAcs75kaWHd+bcXE7Xv1lj2a9WOrdfX6yLjfu18AIfKZfnxsIH4VkneuZ5eGWgENbKjfpiowF/AONMmc4AeJxjYGRgYADiDSv+hcXz23xl4Gd+ARRheLxqSz2C/j+JpZPZCMjlYGACiQIAhQUNJQAAAHicY2BkYGAO+p/FwMDSycAAJhkZUAEbAEzUAuED6AAAAq4AAAOqAAADmAAABIkAAAKzAAAAAAAAAD4AdgDMAUIBjAAAAAEAAAAGACEABAAAAAAAAgAcAEIAjQAAAGQODAAAAAB4nHWQ32rCMBSHf/HfNoVtbLDb5WooY1UL3giC4NCb7UaGt6PW2lZqI2kUfI29wx5mL7Fn2c8ax1DWkuY7X05O0gPgBt8Q2D8djj0LnDPacwFn6Fku0j9bLpFfLJdRw5vlCv275SoeEVqu4RYfrCBKF4wW+LQscC2uLBdwKe4tF+mfLJfIPctl3IlXyxV633IVE5FZruFBfA3UaqvjMDKyPmhIt+V25HQrFVWceon01iZSOpN9OVepCZJEOb5aHngchOvE04fwME8CncUqlW2ndVCjIA20Z4LZrnq2CV1j5nKu1VIObYZcabUIfONExqy6zebf8zCAwgpbaMRsVQQDiTptg7OLFkeHNGWGZOY+K0YKDwmNhzV3RPlKxrjPMWeU0gbMSMgOfH6XJ35MCrk/YRV9snocT0i7M+LcS7RZt3WSNSKleaaX29nv3TNseJpLa7hrd0ud30pieFRDsh+7tQWNT+/kXTG0XTT5/vN/P+NshE94nGNgYoAALgbsgI2RiZGZkYWRlZGNkZ2BLTkxLzk1hy0nNa1E15A1OSc/OZslIz83lQPISizJzM9jYAAA4pgLzgAAAHicY/DewXAiKGIjI2Nf5AbGnRwMHAzJBRsZ2J02MjBoQWguFHonAwMDNxJrJwMzA4PLRhXGjsCIDQ4dESB+istGDRB/BwcDRIDBJVJ6ozpIaBdHAwMji0NHcghMAgQ2MvBp7WD837qBpXcjE4PLZtYUNgYXFwCUHCoHAAA=\x27)\x20format(\x27woff\x27),\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20url(\x27data:application/octet-stream;base64,AAEAAAAPAIAAAwBwR1NVQiCLJXoAAAD8AAAAVE9TLzI+I1IfAAABUAAAAGBjbWFw4af0qQAAAbAAAAGwY3Z0IAAAAAAAAApIAAAADmZwZ21iLvl6AAAKWAAADgxnYXNwAAAAEAAACkAAAAAIZ2x5ZjlmWUQAAANgAAADGGhlYWQrAaynAAAGeAAAADZoaGVhB90D+gAABrAAAAAkaG10eBUUAAAAAAbUAAAAGGxvY2EClgG4AAAG7AAAAA5tYXhwARgOcQAABvwAAAAgbmFtZc2dHB0AAAccAAACzXBvc3QSDgC1AAAJ7AAAAFNwcmVwfrY7tgAAGGQAAACcAAEAAAAKADAAPgACREZMVAAObGF0bgAaAAQAAAAAAAAAAQAAAAQAAAAAAAAAAQAAAAFsaWdhAAgAAAABAAAAAQAEAAQAAAABAAgAAQAGAAAAAQAAAAQDgwGQAAUAAAJ6ArwAAACMAnoCvAAAAeAAMQECAAACAAUDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAFBmRWQAwOgA8DEDUv9qAFoDrACWAAAAAQAAAAAAAAAAAAAAAAACAAAABQAAAAMAAAAsAAAABAAAAWQAAQAAAAAAXgADAAEAAAAsAAMACgAAAWQABAAyAAAABgAEAAEAAugD8DH//wAA6ADwMf//AAAAAAABAAYADAAAAAEAAgADAAQABQAAAQYAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADAAAAAAATAAAAAAAAAAFAADoAAAA6AAAAAABAADoAQAA6AEAAAACAADoAgAA6AIAAAADAADoAwAA6AMAAAAEAADwMQAA8DEAAAAFAAEAAAAAAq4CsgAcAB5AGxgRCgMEAgABTAEBAAIAhQMBAgJ2FBgUFwQGGis1ND8BJyY0NjIfATc2MhYUDwEXFhQGIi8BBwYiJhrDwxo0RhrEwxlIMhnDwxkySBnDxBlIM1okGsPEGUgyGcTEGTJIGcTDGkgyGcPDGTMAAAABAAD/xwOpAvoAFAAkQCEAAAEAhQADAgOGAAECAgFXAAEBAl8AAgECTxQjJBIEBhorEQE2MhYUDwEhMhYUBiMhFxYUDgEnAYYTMiQS8wKCGSQkGf1+8xIkMhMBYQGHEiQ0EfMkMiTzEjQiAhMAAAAAAwAA/5IDmAMqAAgAEQAXAElARhYVFBMEAgQBTAcBBAMCAwQCgAUBAAADBAADaQYBAgEBAlkGAQICAWEAAQIBURISCgkBABIXEhcODQkRChEFBAAIAQgIBhYrATIAEAAgABAAEzI2ECYgBhAWExUXBycRAcy+AQ7+8v6E/vIBDr6W0tL+1tTUuJYyqgMq/vL+hP7yAQ4BfAEO/MzUASrS0v7W1AJs9JYyqgESAAQAAP/QBIkC7AAHAA8AEwAXAJNLsAtQWEA1AAMAA4UABggJCAZyAgEAAAEEAAFnAAQKAQgGBAhnDQsMAwkFBQlXDQsMAwkJBV8HAQUJBU8bQDYAAwADhQAGCAkIBgmAAgEAAAEEAAFnAAQKAQgGBAhnDQsMAwkFBQlXDQsMAwkJBV8HAQUJBU9ZQBoUFBAQFBcUFxYVEBMQExIREREREREREA4GHysBIRUhNSE1IQEhESERIxEjJTUjFSE1IxUDdQEU+3cCJAFR/PcDr/3Iqs0Cn5IBUZICP1parf7N/hcBLf7T9LGxsbEAAAAAAgAA/5QCswMyABcAIAAmQCMAAgMBAwIBgAABAYQAAAMDAFkAAAADYQADAANRExgaFgQGGisRNDc2Nz4BMh4BFxYUBwYHAwYiJwMmJyY3FBYyNjQmIgYbGTEvfo99YBobGxIS5RY+GOQWDhvZS2tLS2tLAdlGQD0yLzU1YD5AjEAoGf6lIyMBWx8iQEY1TEtrTEwAAAEAAAABAACwqP5WXw889QAPA+gAAAAA46q0fwAAAADjqrR/AAD/kgSJAzIAAAAIAAIAAAAAAAAAAQAAA1L/agAABIkAAAAABIkAAQAAAAAAAAAAAAAAAAAAAAYD6AAAAq4AAAOqAAADmAAABIkAAAKzAAAAAAAAAD4AdgDMAUIBjAAAAAEAAAAGACEABAAAAAAAAgAcAEIAjQAAAGQODAAAAAAAAAASAN4AAQAAAAAAAAA1AAAAAQAAAAAAAQAIADUAAQAAAAAAAgAHAD0AAQAAAAAAAwAIAEQAAQAAAAAABAAIAEwAAQAAAAAABQALAFQAAQAAAAAABgAIAF8AAQAAAAAACgArAGcAAQAAAAAACwATAJIAAwABBAkAAABqAKUAAwABBAkAAQAQAQ8AAwABBAkAAgAOAR8AAwABBAkAAwAQAS0AAwABBAkABAAQAT0AAwABBAkABQAWAU0AAwABBAkABgAQAWMAAwABBAkACgBWAXMAAwABBAkACwAmAclDb3B5cmlnaHQgKEMpIDIwMjUgYnkgb3JpZ2luYWwgYXV0aG9ycyBAIGZvbnRlbGxvLmNvbWZvbnRlbGxvUmVndWxhcmZvbnRlbGxvZm9udGVsbG9WZXJzaW9uIDEuMGZvbnRlbGxvR2VuZXJhdGVkIGJ5IHN2ZzJ0dGYgZnJvbSBGb250ZWxsbyBwcm9qZWN0Lmh0dHA6Ly9mb250ZWxsby5jb20AQwBvAHAAeQByAGkAZwBoAHQAIAAoAEMAKQAgADIAMAAyADUAIABiAHkAIABvAHIAaQBnAGkAbgBhAGwAIABhAHUAdABoAG8AcgBzACAAQAAgAGYAbwBuAHQAZQBsAGwAbwAuAGMAbwBtAGYAbwBuAHQAZQBsAGwAbwBSAGUAZwB1AGwAYQByAGYAbwBuAHQAZQBsAGwAbwBmAG8AbgB0AGUAbABsAG8AVgBlAHIAcwBpAG8AbgAgADEALgAwAGYAbwBuAHQAZQBsAGwAbwBHAGUAbgBlAHIAYQB0AGUAZAAgAGIAeQAgAHMAdgBnADIAdAB0AGYAIABmAHIAbwBtACAARgBvAG4AdABlAGwAbABvACAAcAByAG8AagBlAGMAdAAuAGgAdAB0AHAAOgAvAC8AZgBvAG4AdABlAGwAbABvAC4AYwBvAG0AAAAAAgAAAAAAAAAKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGAQIBAwEEAQUBBgEHAAZjYW5jZWwGbGVmdC0xBWNsb2NrBGhvbWUIbG9jYXRpb24AAAAAAQAB//8ADwAAAAAAAAAAAAAAAAAAAACwACwgsABVWEVZICBLuAAOUUuwBlNaWLA0G7AoWWBmIIpVWLACJWG5CAAIAGNjI2IbISGwAFmwAEMjRLIAAQBDYEItsAEssCBgZi2wAiwjISMhLbADLCBkswMUFQBCQ7ATQyBgYEKxAhRDQrElA0OwAkNUeCCwDCOwAkNDYWSwBFB4sgICAkNgQrAhZRwhsAJDQ7IOFQFCHCCwAkMjQrITARNDYEIjsABQWGVZshYBAkNgQi2wBCywAyuwFUNYIyEjIbAWQ0MjsABQWGVZGyBkILDAULAEJlqyKAENQ0VjRbAGRVghsAMlWVJbWCEjIRuKWCCwUFBYIbBAWRsgsDhQWCGwOFlZILEBDUNFY0VhZLAoUFghsQENQ0VjRSCwMFBYIbAwWRsgsMBQWCBmIIqKYSCwClBYYBsgsCBQWCGwCmAbILA2UFghsDZgG2BZWVkbsAIlsAxDY7AAUliwAEuwClBYIbAMQxtLsB5QWCGwHkthuBAAY7AMQ2O4BQBiWVlkYVmwAStZWSOwAFBYZVlZIGSwFkMjQlktsAUsIEUgsAQlYWQgsAdDUFiwByNCsAgjQhshIVmwAWAtsAYsIyEjIbADKyBksQdiQiCwCCNCsAZFWBuxAQ1DRWOxAQ1DsABgRWOwBSohILAIQyCKIIqwASuxMAUlsAQmUVhgUBthUllYI1khWSCwQFNYsAErGyGwQFkjsABQWGVZLbAHLLAJQyuyAAIAQ2BCLbAILLAJI0IjILAAI0JhsAJiZrABY7ABYLAHKi2wCSwgIEUgsA5DY7gEAGIgsABQWLBAYFlmsAFjYESwAWAtsAossgkOAENFQiohsgABAENgQi2wCyywAEMjRLIAAQBDYEItsAwsICBFILABKyOwAEOwBCVgIEWKI2EgZCCwIFBYIbAAG7AwUFiwIBuwQFlZI7AAUFhlWbADJSNhRESwAWAtsA0sICBFILABKyOwAEOwBCVgIEWKI2EgZLAkUFiwABuwQFkjsABQWGVZsAMlI2FERLABYC2wDiwgsAAjQrMNDAADRVBYIRsjIVkqIS2wDyyxAgJFsGRhRC2wECywAWAgILAPQ0qwAFBYILAPI0JZsBBDSrAAUlggsBAjQlktsBEsILAQYmawAWMguAQAY4ojYbARQ2AgimAgsBEjQiMtsBIsS1RYsQRkRFkksA1lI3gtsBMsS1FYS1NYsQRkRFkbIVkksBNlI3gtsBQssQASQ1VYsRISQ7ABYUKwEStZsABDsAIlQrEPAiVCsRACJUKwARYjILADJVBYsQEAQ2CwBCVCioogiiNhsBAqISOwAWEgiiNhsBAqIRuxAQBDYLACJUKwAiVhsBAqIVmwD0NHsBBDR2CwAmIgsABQWLBAYFlmsAFjILAOQ2O4BABiILAAUFiwQGBZZrABY2CxAAATI0SwAUOwAD6yAQEBQ2BCLbAVLACxAAJFVFiwEiNCIEWwDiNCsA0jsABgQiBgtxgYAQARABMAQkJCimAgsBQjQrABYbEUCCuwiysbIlktsBYssQAVKy2wFyyxARUrLbAYLLECFSstsBkssQMVKy2wGiyxBBUrLbAbLLEFFSstsBwssQYVKy2wHSyxBxUrLbAeLLEIFSstsB8ssQkVKy2wKywjILAQYmawAWOwBmBLVFgjIC6wAV0bISFZLbAsLCMgsBBiZrABY7AWYEtUWCMgLrABcRshIVktsC0sIyCwEGJmsAFjsCZgS1RYIyAusAFyGyEhWS2wICwAsA8rsQACRVRYsBIjQiBFsA4jQrANI7AAYEIgYLABYbUYGAEAEQBCQopgsRQIK7CLKxsiWS2wISyxACArLbAiLLEBICstsCMssQIgKy2wJCyxAyArLbAlLLEEICstsCYssQUgKy2wJyyxBiArLbAoLLEHICstsCkssQggKy2wKiyxCSArLbAuLCA8sAFgLbAvLCBgsBhgIEMjsAFgQ7ACJWGwAWCwLiohLbAwLLAvK7AvKi2wMSwgIEcgILAOQ2O4BABiILAAUFiwQGBZZrABY2AjYTgjIIpVWCBHICCwDkNjuAQAYiCwAFBYsEBgWWawAWNgI2E4GyFZLbAyLACxAAJFVFixDgZFQrABFrAxKrEFARVFWDBZGyJZLbAzLACwDyuxAAJFVFixDgZFQrABFrAxKrEFARVFWDBZGyJZLbA0LCA1sAFgLbA1LACxDgZFQrABRWO4BABiILAAUFiwQGBZZrABY7ABK7AOQ2O4BABiILAAUFiwQGBZZrABY7ABK7AAFrQAAAAAAEQ+IzixNAEVKiEtsDYsIDwgRyCwDkNjuAQAYiCwAFBYsEBgWWawAWNgsABDYTgtsDcsLhc8LbA4LCA8IEcgsA5DY7gEAGIgsABQWLBAYFlmsAFjYLAAQ2GwAUNjOC2wOSyxAgAWJSAuIEewACNCsAIlSYqKRyNHI2EgWGIbIVmwASNCsjgBARUUKi2wOiywABawFyNCsAQlsAQlRyNHI2GxDABCsAtDK2WKLiMgIDyKOC2wOyywABawFyNCsAQlsAQlIC5HI0cjYSCwBiNCsQwAQrALQysgsGBQWCCwQFFYswQgBSAbswQmBRpZQkIjILAKQyCKI0cjRyNhI0ZgsAZDsAJiILAAUFiwQGBZZrABY2AgsAErIIqKYSCwBENgZCOwBUNhZFBYsARDYRuwBUNgWbADJbACYiCwAFBYsEBgWWawAWNhIyAgsAQmI0ZhOBsjsApDRrACJbAKQ0cjRyNhYCCwBkOwAmIgsABQWLBAYFlmsAFjYCMgsAErI7AGQ2CwASuwBSVhsAUlsAJiILAAUFiwQGBZZrABY7AEJmEgsAQlYGQjsAMlYGRQWCEbIyFZIyAgsAQmI0ZhOFktsDwssAAWsBcjQiAgILAFJiAuRyNHI2EjPDgtsD0ssAAWsBcjQiCwCiNCICAgRiNHsAErI2E4LbA+LLAAFrAXI0KwAyWwAiVHI0cjYbAAVFguIDwjIRuwAiWwAiVHI0cjYSCwBSWwBCVHI0cjYbAGJbAFJUmwAiVhuQgACABjYyMgWGIbIVljuAQAYiCwAFBYsEBgWWawAWNgIy4jICA8ijgjIVktsD8ssAAWsBcjQiCwCkMgLkcjRyNhIGCwIGBmsAJiILAAUFiwQGBZZrABYyMgIDyKOC2wQCwjIC5GsAIlRrAXQ1hQG1JZWCA8WS6xMAEUKy2wQSwjIC5GsAIlRrAXQ1hSG1BZWCA8WS6xMAEUKy2wQiwjIC5GsAIlRrAXQ1hQG1JZWCA8WSMgLkawAiVGsBdDWFIbUFlYIDxZLrEwARQrLbBDLLA6KyMgLkawAiVGsBdDWFAbUllYIDxZLrEwARQrLbBELLA7K4ogIDywBiNCijgjIC5GsAIlRrAXQ1hQG1JZWCA8WS6xMAEUK7AGQy6wMCstsEUssAAWsAQlsAQmICAgRiNHYbAMI0IuRyNHI2GwC0MrIyA8IC4jOLEwARQrLbBGLLEKBCVCsAAWsAQlsAQlIC5HI0cjYSCwBiNCsQwAQrALQysgsGBQWCCwQFFYswQgBSAbswQmBRpZQkIjIEewBkOwAmIgsABQWLBAYFlmsAFjYCCwASsgiophILAEQ2BkI7AFQ2FkUFiwBENhG7AFQ2BZsAMlsAJiILAAUFiwQGBZZrABY2GwAiVGYTgjIDwjOBshICBGI0ewASsjYTghWbEwARQrLbBHLLEAOisusTABFCstsEgssQA7KyEjICA8sAYjQiM4sTABFCuwBkMusDArLbBJLLAAFSBHsAAjQrIAAQEVFBMusDYqLbBKLLAAFSBHsAAjQrIAAQEVFBMusDYqLbBLLLEAARQTsDcqLbBMLLA5Ki2wTSywABZFIyAuIEaKI2E4sTABFCstsE4ssAojQrBNKy2wTyyyAABGKy2wUCyyAAFGKy2wUSyyAQBGKy2wUiyyAQFGKy2wUyyyAABHKy2wVCyyAAFHKy2wVSyyAQBHKy2wViyyAQFHKy2wVyyzAAAAQystsFgsswABAEMrLbBZLLMBAABDKy2wWiyzAQEAQystsFssswAAAUMrLbBcLLMAAQFDKy2wXSyzAQABQystsF4sswEBAUMrLbBfLLIAAEUrLbBgLLIAAUUrLbBhLLIBAEUrLbBiLLIBAUUrLbBjLLIAAEgrLbBkLLIAAUgrLbBlLLIBAEgrLbBmLLIBAUgrLbBnLLMAAABEKy2waCyzAAEARCstsGksswEAAEQrLbBqLLMBAQBEKy2wayyzAAABRCstsGwsswABAUQrLbBtLLMBAAFEKy2wbiyzAQEBRCstsG8ssQA8Ky6xMAEUKy2wcCyxADwrsEArLbBxLLEAPCuwQSstsHIssAAWsQA8K7BCKy2wcyyxATwrsEArLbB0LLEBPCuwQSstsHUssAAWsQE8K7BCKy2wdiyxAD0rLrEwARQrLbB3LLEAPSuwQCstsHgssQA9K7BBKy2weSyxAD0rsEIrLbB6LLEBPSuwQCstsHsssQE9K7BBKy2wfCyxAT0rsEIrLbB9LLEAPisusTABFCstsH4ssQA+K7BAKy2wfyyxAD4rsEErLbCALLEAPiuwQistsIEssQE+K7BAKy2wgiyxAT4rsEErLbCDLLEBPiuwQistsIQssQA/Ky6xMAEUKy2whSyxAD8rsEArLbCGLLEAPyuwQSstsIcssQA/K7BCKy2wiCyxAT8rsEArLbCJLLEBPyuwQSstsIossQE/K7BCKy2wiyyyCwADRVBYsAYbsgQCA0VYIyEbIVlZQiuwCGWwAyRQeLEFARVFWDBZLQBLuADIUlixAQGOWbABuQgACABjcLEAB0KxAAAqsQAHQrEACiqxAAdCsQAKKrEAB0K5AAAACyqxAAdCuQAAAAsquQADAABEsSQBiFFYsECIWLkAAwBkRLEoAYhRWLgIAIhYuQADAABEWRuxJwGIUVi6CIAAAQRAiGNUWLkAAwAARFlZWVlZsQAOKrgB/4WwBI2xAgBEswVkBgBERA==\x27)\x20format(\x27truetype\x27);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-list-item\x20.ga-autocomplete-location-icon:before\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-family:\x20var(--ga-autocomplete-location-icon-font-familiy,getAddress);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20content:\x20\x27\x5cf031\x27;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20color:\x20var(--ga-autocomplete-location-item-color,#EA4335);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-history-item\x20.ga-autocomplete-location-icon:before\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-family:\x20var(--ga-autocomplete-location-icon-font-familiy,getAddress);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20content:\x20\x27\x5ce802\x27;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20color:\x20var(--ga-autocomplete-location-history-item-color,grey);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}';return _0x2717bd;},this['list']['id']='ga-autocomplete-list-'+_0x37e105,this['list']['setAttribute']('role','listbox'),this['list']['addEventListener']('keydown',this['handleKeyDown']),this['list']['className']='ga-autocomplete-list',this['ul']['classList']['add']('ga-autocomplete-scroll'),this['list']['appendChild'](this['ul']),Storage['clearExpired']();}['destroy'](){this['list']['removeEventListener']('keydown',this['handleKeyDown']),this['list']['remove'](),this['style']['remove']();}static['setFocus'](_0x32e5c2,_0x403e92,_0xee602b){if(_0x32e5c2>-0x1&&_0x403e92['ul']['children']['length']>0x0){List['removeFocusedClassName'](_0x403e92);let _0x15f3d9=_0x403e92['ul']['children'][_0x32e5c2];List['addFocusedClassName'](_0x15f3d9),_0x15f3d9['focus']({'focusVisible':!![]});return;}_0xee602b['setFocus']();}['resetSelectedIndex'](){this['removeFocusedClassName'](),this['selectedIndex']=-0x1;}static['setSelectedIndex'](_0x5ab347,_0x48b4a6,_0x1a6d69){if(_0x5ab347==-0x1){_0x1a6d69['setFocus']();return;}if(_0x5ab347>_0x48b4a6['lastIndex']()){_0x48b4a6['selectedIndex']=_0x48b4a6['lastIndex']();return;}_0x48b4a6['selectedIndex']=_0x5ab347,_0x48b4a6['setFocus']();}['lastIndex'](){let _0x36b62b=0x1;return this['options']['footer_template']&&(_0x36b62b=0x2),this['ul']['children']['length']-_0x36b62b;}['getSelectedIndex'](){return this['selectedIndex'];}['containsActiveElment'](){return this['ul']['contains'](document['activeElement']);}['dispatchSelected'](_0x344837,_0x100209,_0x17c55d){const _0x383bea=_0x100209,_0x99953e=new Event('getaddress-autocomplete-selected',{'bubbles':!![]});_0x99953e['address']=_0x17c55d,_0x99953e['data']=_0x383bea,_0x99953e['id']=_0x344837,this['element']['dispatchEvent'](_0x99953e),this['options']['selected']&&this['options']['selected'](_0x17c55d);}['dispatchSelectedFailed'](_0x2122c,_0xd59ff6,_0x3df75f){const _0x23c5c8=new Event('getaddress-autocomplete-selected-failed',{'bubbles':!![]});_0x23c5c8['status']=_0xd59ff6,_0x23c5c8['message']=_0x3df75f,_0x23c5c8['id']=_0x2122c,this['element']['dispatchEvent'](_0x23c5c8),this['options']['selected_failed']&&this['options']['selected_failed'](_0x2122c,_0xd59ff6,_0x3df75f);}['dispatchSuggestions'](_0x35853f,_0x2e0640){const _0x22f194=_0x35853f,_0x354324=new Event('getaddress-autocomplete-suggestions',{'bubbles':!![]});_0x354324['suggestions']=_0x2e0640,_0x354324['data']=_0x22f194,this['element']['dispatchEvent'](_0x354324),this['options']['suggested']&&this['options']['suggested'](_0x2e0640);}['dispatchSuggestionsFailed'](_0x3e27ae,_0x57733e,_0x4f2315){const _0x40b41a=new Event('getaddress-autocomplete-suggestions-failed',{'bubbles':!![]});_0x40b41a['status']=_0x57733e,_0x40b41a['message']=_0x4f2315,_0x40b41a['data']=_0x3e27ae,this['element']['dispatchEvent'](_0x40b41a),this['options']['suggested_failed']&&this['options']['suggested_failed'](_0x3e27ae,_0x57733e,_0x4f2315);}}List['addFocusedClassName']=_0x5dea46=>{_0x5dea46['classList']['add']('ga-autocomplete-list-item-focused');},List['removeFocusedClassName']=_0x4cbc97=>{const _0x2e243c=_0x4cbc97['ul']['children'];for(let _0x5826be=0x0;_0x5826be<_0x2e243c['length'];_0x5826be++){_0x2e243c[_0x5826be]['classList']['remove']('ga-autocomplete-list-item-focused');}};

    class ItemContainer{constructor(_0xf90240,_0x27fa2b){this['input']=_0xf90240,this['index']=_0x27fa2b,this['container']=document['createElement']('DIV'),this['element']=this['container'],this['handleFocus']=_0x258084=>{this['input']['list']['setSelectedIndex'](this['index']);},this['handleKeyDown']=_0x4ae5aa=>{switch(_0x4ae5aa['key']){case'ArrowUp':this['handleUpKey'](_0x4ae5aa);break;case'ArrowDown':this['handleDownKey'](_0x4ae5aa);break;case'Enter':this['handleEnterKey'](_0x4ae5aa);break;}},this['handleDownKey']=_0x22cd30=>{this['input']['list']['setSelectedIndex'](this['index']+0x1),_0x22cd30['preventDefault']();},this['handleUpKey']=_0x302c35=>{this['input']['list']['setSelectedIndex'](this['index']-0x1),_0x302c35['preventDefault']();},this['container']['addEventListener']('keydown',this['handleKeyDown']),this['container']['addEventListener']('focus',this['handleFocus']),this['container']['tabIndex']=-0x1,this['container']['className']='ga-autocomplete-list-item';}['destroy'](){this['container']['removeEventListener']('keydown',this['handleKeyDown']),this['container']['removeEventListener']('focus',this['handleFocus']),this['container']['remove']();}}ItemContainer['getAddress']=(_0x89518e,_0x3135d3,_0x51b023)=>{_0x89518e['dispatchSelected'](_0x3135d3['id'],_0x51b023),_0x89518e['clearList']();};

    var __awaiter$b=undefined&&undefined['__awaiter']||function(_0x284a20,_0x4aa4b3,_0x194c9f,_0x152ce9){function _0x510206(_0x362df9){return _0x362df9 instanceof _0x194c9f?_0x362df9:new _0x194c9f(function(_0x171637){_0x171637(_0x362df9);});}return new(_0x194c9f||(_0x194c9f=Promise))(function(_0x4f0801,_0x42678d){function _0x251e6d(_0x3e50f9){try{_0x4b73ea(_0x152ce9['next'](_0x3e50f9));}catch(_0x4723ad){_0x42678d(_0x4723ad);}}function _0x5d3c6d(_0x299997){try{_0x4b73ea(_0x152ce9['throw'](_0x299997));}catch(_0x295837){_0x42678d(_0x295837);}}function _0x4b73ea(_0x27a759){_0x27a759['done']?_0x4f0801(_0x27a759['value']):_0x510206(_0x27a759['value'])['then'](_0x251e6d,_0x5d3c6d);}_0x4b73ea((_0x152ce9=_0x152ce9['apply'](_0x284a20,_0x4aa4b3||[]))['next']());});};class AnchoredItemContainer extends ItemContainer{constructor(_0x136ba4,_0x3e86f3,_0x5c3394,_0x38363c,_0x2910d8){super(_0x3e86f3,_0x2910d8),this['client']=_0x136ba4,this['suggestion']=_0x5c3394,this['options']=_0x38363c,this['handleEnterKey']=_0x4db340=>__awaiter$b(this,void 0x0,void 0x0,function*(){_0x4db340['preventDefault'](),yield this['getAddress'](),this['input']['repopulate']=![],this['input']['setFocus']();}),this['destroy']=()=>{this['container']['removeEventListener']('click',this['handleClick']),super['destroy']();},this['handleClick']=_0x54777c=>__awaiter$b(this,void 0x0,void 0x0,function*(){yield this['getAddress'](),this['input']['repopulate']=![],this['input']['setFocus']();}),this['getAddress']=()=>__awaiter$b(this,void 0x0,void 0x0,function*(){const _0x27f889=yield this['client']['get'](this['suggestion']['id']);if(!_0x27f889['isSuccess']){const _0x60cd70=_0x27f889['toFailed']();this['input']['dispatchSelectedFailed'](this['suggestion']['id'],_0x60cd70['status'],_0x60cd70['message']);return;}let _0x3f40d6=_0x27f889['toSuccess']();this['options']['enable_history']&&Storage['save'](this['suggestion'],_0x3f40d6['address']),ItemContainer['getAddress'](this['input'],this['suggestion'],_0x3f40d6['address']);}),this['container']['innerHTML']=_0x38363c['item_template'](_0x5c3394),this['container']['classList']['add']('ga-autocomplete-anchored-list-item'),_0x38363c['list_item_style']&&this['container']['setAttribute']('style',_0x38363c['list_item_style']),this['container']['addEventListener']('click',this['handleClick']);}}

    var __awaiter$a=undefined&&undefined['__awaiter']||function(_0x138328,_0x39e32c,_0x139690,_0x3e0e3f){function _0x565233(_0x47b739){return _0x47b739 instanceof _0x139690?_0x47b739:new _0x139690(function(_0x54d730){_0x54d730(_0x47b739);});}return new(_0x139690||(_0x139690=Promise))(function(_0x26b247,_0x5f4202){function _0x4c7f6c(_0x2db098){try{_0x5bb034(_0x3e0e3f['next'](_0x2db098));}catch(_0x302af3){_0x5f4202(_0x302af3);}}function _0x2528a5(_0x423424){try{_0x5bb034(_0x3e0e3f['throw'](_0x423424));}catch(_0x39914f){_0x5f4202(_0x39914f);}}function _0x5bb034(_0x4896c7){_0x4896c7['done']?_0x26b247(_0x4896c7['value']):_0x565233(_0x4896c7['value'])['then'](_0x4c7f6c,_0x2528a5);}_0x5bb034((_0x3e0e3f=_0x3e0e3f['apply'](_0x138328,_0x39e32c||[]))['next']());});};class AnchoredHistoryContainer extends ItemContainer{constructor(_0x1342e0,_0x482941,_0x3096fa,_0x442f29){var _0x37a2f7;super(_0x1342e0,_0x442f29),this['storedAddress']=_0x482941,this['options']=_0x3096fa,this['handleEnterKey']=_0x408026=>__awaiter$a(this,void 0x0,void 0x0,function*(){_0x408026['preventDefault'](),yield this['getAddress'](),this['input']['repopulate']=![],this['input']['setFocus']();}),this['destroy']=()=>{this['container']['removeEventListener']('click',this['handleClick']),super['destroy']();},this['handleClick']=_0x4f4083=>__awaiter$a(this,void 0x0,void 0x0,function*(){yield this['getAddress'](),this['input']['repopulate']=![],this['input']['setFocus']();}),this['getAddress']=()=>__awaiter$a(this,void 0x0,void 0x0,function*(){yield ItemContainer['getAddress'](this['input'],this['storedAddress']['suggestion'],this['storedAddress']['address']);});const _0x132416=(_0x37a2f7=_0x3096fa['history_item_template'])!==null&&_0x37a2f7!==void 0x0?_0x37a2f7:_0x3096fa['item_template'];this['container']['innerHTML']=_0x132416(_0x482941['suggestion']),this['container']['classList']['add']('ga-autocomplete-history-item'),this['container']['classList']['add']('ga-autocomplete-anchored-history-item'),_0x3096fa['history_item_style']&&this['container']['setAttribute']('style',_0x3096fa['history_item_style']),this['container']['addEventListener']('click',this['handleClick']);}}

    /* eslint-disable class-methods-use-this */
    class Result {
        constructor(isSuccess) {
            this.isSuccess = isSuccess;
        }
    }
    class Success extends Result {
        constructor() {
            super(true);
        }
    }
    class AutocompleteSuccess extends Success {
        constructor(suggestions) {
            super();
            this.suggestions = suggestions;
        }
        toSuccess() {
            return this;
        }
        toFailed() {
            throw new Error('Did not fail');
        }
    }
    class LocationSuccess extends Success {
        constructor(suggestions) {
            super();
            this.suggestions = suggestions;
        }
        toSuccess() {
            return this;
        }
        toFailed() {
            throw new Error('Did not fail');
        }
    }
    class GetSuccess extends Success {
        constructor(address) {
            super();
            this.address = address;
        }
        toSuccess() {
            return this;
        }
        toFailed() {
            throw new Error('Did not fail');
        }
    }
    class GetLocationSuccess extends Success {
        constructor(location) {
            super();
            this.location = location;
        }
        toSuccess() {
            return this;
        }
        toFailed() {
            throw new Error('Did not fail');
        }
    }
    class GetLocationFailed extends Result {
        constructor(status, message) {
            super(false);
            this.status = status;
            this.message = message;
        }
        toSuccess() {
            throw new Error('Not a success');
        }
        toFailed() {
            return this;
        }
    }
    class AutocompleteFailed extends Result {
        constructor(status, message) {
            super(false);
            this.status = status;
            this.message = message;
        }
        toSuccess() {
            throw new Error('Not a success');
        }
        toFailed() {
            return this;
        }
    }
    class LocationFailed extends Result {
        constructor(status, message) {
            super(false);
            this.status = status;
            this.message = message;
        }
        toSuccess() {
            throw new Error('Not a success');
        }
        toFailed() {
            return this;
        }
    }
    class GetFailed extends Result {
        constructor(status, message) {
            super(false);
            this.status = status;
            this.message = message;
        }
        toSuccess() {
            throw new Error('Not a success');
        }
        toFailed() {
            return this;
        }
    }
    class FindSuccess extends Success {
        constructor(addresses) {
            super();
            this.addresses = addresses;
        }
        toSuccess() {
            return this;
        }
        toFailed() {
            throw new Error('failed');
        }
    }
    class FindFailed extends Result {
        constructor(status, message) {
            super(false);
            this.status = status;
            this.message = message;
        }
        toSuccess() {
            throw new Error('failed');
        }
        toFailed() {
            return this;
        }
    }
    class TypeaheadSuccess extends Success {
        constructor(results) {
            super();
            this.results = results;
        }
        toSuccess() {
            return this;
        }
        toFailed() {
            throw new Error('failed');
        }
    }
    class TypeaheadFailed extends Result {
        constructor(status, message) {
            super(false);
            this.status = status;
            this.message = message;
        }
        toSuccess() {
            throw new Error('failed');
        }
        toFailed() {
            return this;
        }
    }

    var __awaiter$9 = function (thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    };
    class Client {
        constructor(api_key, autocomplete_url = 'https://api.getaddress.io/autocomplete/{query}', get_url = 'https://api.getaddress.io/get/{id}', location_url = 'https://api.getaddress.io/location/{query}', get_location_url = 'https://api.getaddress.io/get-location/{id}', typeahead_url = 'https://api.getaddress.io/typeahead/{term}') {
            this.api_key = api_key;
            this.autocomplete_url = autocomplete_url;
            this.get_url = get_url;
            this.location_url = location_url;
            this.get_location_url = get_location_url;
            this.typeahead_url = typeahead_url;
            this.autocompleteResponse = undefined;
            this.getResponse = undefined;
            this.locationResponse = undefined;
            this.getLocationResponse = undefined;
            this.typeaheadResponse = undefined;
            this.autocompleteAbortController = new AbortController();
            this.getAbortController = new AbortController();
            this.typeaheadAbortController = new AbortController();
            this.locationAbortController = new AbortController();
            this.getLocationAbortController = new AbortController();
        }
        location(query_1) {
            return __awaiter$9(this, arguments, void 0, function* (query, options = {}) {
                try {
                    const combinedOptions = Object.assign({ all: true }, options);
                    let url = this.location_url.replace(/{query}/i, query);
                    if (this.api_key) {
                        if (url.includes('?')) {
                            url = `${url}&api-key=${this.api_key}`;
                        }
                        else {
                            url = `${url}?api-key=${this.api_key}`;
                        }
                    }
                    if (this.locationResponse !== undefined) {
                        this.locationResponse = undefined;
                        this.locationAbortController.abort();
                        this.locationAbortController = new AbortController();
                    }
                    this.locationResponse = yield fetch(url, {
                        method: 'post',
                        signal: this.locationAbortController.signal,
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify(combinedOptions),
                    });
                    if (this.locationResponse.status === 200) {
                        const json = yield this.locationResponse.json();
                        const suggestions = json.suggestions;
                        return new LocationSuccess(suggestions);
                    }
                    const json = yield this.locationResponse.json();
                    return new LocationFailed(this.locationResponse.status, json.Message);
                }
                catch (err) {
                    if (err instanceof Error) {
                        if (err.name === 'AbortError') {
                            return new LocationSuccess([]);
                        }
                        return new LocationFailed(401, err.message);
                    }
                    return new LocationFailed(401, 'Unauthorised');
                }
                finally {
                    this.locationResponse = undefined;
                }
            });
        }
        getLocation(id) {
            return __awaiter$9(this, void 0, void 0, function* () {
                try {
                    let url = this.get_location_url.replace(/{id}/i, id);
                    if (this.api_key) {
                        if (url.includes('?')) {
                            url = `${url}&api-key=${this.api_key}`;
                        }
                        else {
                            url = `${url}?api-key=${this.api_key}`;
                        }
                    }
                    if (this.getLocationResponse !== undefined) {
                        this.getLocationResponse = undefined;
                        this.getLocationAbortController.abort();
                        this.getLocationAbortController = new AbortController();
                    }
                    this.getLocationResponse = yield fetch(url, {
                        method: 'get',
                        signal: this.getLocationAbortController.signal,
                        headers: {
                            'Content-Type': 'application/json',
                        },
                    });
                    if (this.getLocationResponse.status === 200) {
                        const json = yield this.getLocationResponse.json();
                        const loaction = json;
                        return new GetLocationSuccess(loaction);
                    }
                    const json = yield this.getLocationResponse.json();
                    return new GetLocationFailed(this.getLocationResponse.status, json.Message);
                }
                catch (err) {
                    if (err instanceof Error) {
                        return new GetLocationFailed(401, err.message);
                    }
                    return new GetLocationFailed(401, 'Unauthorised');
                }
                finally {
                    this.getResponse = undefined;
                }
            });
        }
        autocomplete(query_1) {
            return __awaiter$9(this, arguments, void 0, function* (query, options = {}) {
                try {
                    const combinedOptions = Object.assign({ all: true }, options);
                    let url = this.autocomplete_url.replace(/{query}/i, query);
                    if (this.api_key) {
                        if (url.includes('?')) {
                            url = `${url}&api-key=${this.api_key}`;
                        }
                        else {
                            url = `${url}?api-key=${this.api_key}`;
                        }
                    }
                    if (this.autocompleteResponse !== undefined) {
                        this.autocompleteResponse = undefined;
                        this.autocompleteAbortController.abort();
                        this.autocompleteAbortController = new AbortController();
                    }
                    this.autocompleteResponse = yield fetch(url, {
                        method: 'post',
                        signal: this.autocompleteAbortController.signal,
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify(combinedOptions)
                    });
                    if (this.autocompleteResponse.status === 200) {
                        const json = yield this.autocompleteResponse.json();
                        const suggestions = json.suggestions;
                        return new AutocompleteSuccess(suggestions);
                    }
                    const json = yield this.autocompleteResponse.json();
                    return new AutocompleteFailed(this.autocompleteResponse.status, json.Message);
                }
                catch (err) {
                    if (err instanceof Error) {
                        if (err.name === 'AbortError') {
                            return new AutocompleteSuccess([]);
                        }
                        return new AutocompleteFailed(401, err.message);
                    }
                    return new AutocompleteFailed(401, 'Unauthorised');
                }
                finally {
                    this.autocompleteResponse = undefined;
                }
            });
        }
        get(id_1) {
            return __awaiter$9(this, arguments, void 0, function* (id, options = {}) {
                try {
                    let url = this.get_url.replace(/{id}/i, id);
                    if (this.api_key) {
                        if (url.includes('?')) {
                            url = `${url}&api-key=${this.api_key}`;
                        }
                        else {
                            url = `${url}?api-key=${this.api_key}`;
                        }
                    }
                    if (options.remember === false) {
                        url = `${url}&remember=false`;
                    }
                    if (this.getResponse !== undefined) {
                        this.getResponse = undefined;
                        this.getAbortController.abort();
                        this.getAbortController = new AbortController();
                    }
                    this.getResponse = yield fetch(url, {
                        method: 'get',
                        signal: this.getAbortController.signal,
                        headers: {
                            'Content-Type': 'application/json',
                        },
                    });
                    if (this.getResponse.status === 200) {
                        const json = yield this.getResponse.json();
                        const address = json;
                        return new GetSuccess(address);
                    }
                    const json = yield this.getResponse.json();
                    return new GetFailed(this.getResponse.status, json.Message);
                }
                catch (err) {
                    if (err instanceof Error) {
                        return new GetFailed(401, err.message);
                    }
                    return new GetFailed(401, 'Unauthorised');
                }
                finally {
                    this.getResponse = undefined;
                }
            });
        }
        find(postcode) {
            return __awaiter$9(this, void 0, void 0, function* () {
                try {
                    const response = yield fetch(`https://api.getaddress.io/find/${postcode}?api-key=${this.api_key}&expand=true`);
                    if (response.status === 200) {
                        const json = yield response.json();
                        const addresses = json;
                        return new FindSuccess(addresses);
                    }
                    const json = yield response.json();
                    return new FindFailed(response.status, json.Message);
                }
                catch (err) {
                    if (err instanceof Error) {
                        return new FindFailed(401, err.message);
                    }
                    return new FindFailed(401, 'Unauthorised');
                }
            });
        }
        typeahead(term_1) {
            return __awaiter$9(this, arguments, void 0, function* (term, options = {}) {
                try {
                    let url = this.typeahead_url.replace(/{term}/i, term);
                    if (this.api_key) {
                        if (url.includes('?')) {
                            url = `${url}&api-key=${this.api_key}`;
                        }
                        else {
                            url = `${url}?api-key=${this.api_key}`;
                        }
                    }
                    if (this.typeaheadResponse !== undefined) {
                        this.typeaheadResponse = undefined;
                        this.typeaheadAbortController.abort();
                        this.typeaheadAbortController = new AbortController();
                    }
                    this.typeaheadResponse = yield fetch(url, {
                        method: 'post',
                        signal: this.typeaheadAbortController.signal,
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify(options),
                    });
                    if (this.typeaheadResponse.status === 200) {
                        const json = yield this.typeaheadResponse.json();
                        const results = json;
                        return new TypeaheadSuccess(results);
                    }
                    const json = yield this.typeaheadResponse.json();
                    return new TypeaheadFailed(this.typeaheadResponse.status, json.Message);
                }
                catch (err) {
                    if (err instanceof Error) {
                        if (err.name === 'AbortError') {
                            return new TypeaheadSuccess([]);
                        }
                        return new TypeaheadFailed(401, err.message);
                    }
                    return new TypeaheadFailed(401, 'Unauthorised');
                }
                finally {
                    this.typeaheadResponse = undefined;
                }
            });
        }
    }

    var __awaiter$8=undefined&&undefined['__awaiter']||function(_0xa15ac2,_0x50f937,_0x5dea84,_0x229998){function _0x4a0346(_0x428675){return _0x428675 instanceof _0x5dea84?_0x428675:new _0x5dea84(function(_0x620cfa){_0x620cfa(_0x428675);});}return new(_0x5dea84||(_0x5dea84=Promise))(function(_0x21c860,_0xa28ff2){function _0xdd6e5b(_0x2a3c2a){try{_0x315172(_0x229998['next'](_0x2a3c2a));}catch(_0x538fae){_0xa28ff2(_0x538fae);}}function _0x163cff(_0x37b94b){try{_0x315172(_0x229998['throw'](_0x37b94b));}catch(_0x5da4b7){_0xa28ff2(_0x5da4b7);}}function _0x315172(_0x457185){_0x457185['done']?_0x21c860(_0x457185['value']):_0x4a0346(_0x457185['value'])['then'](_0xdd6e5b,_0x163cff);}_0x315172((_0x229998=_0x229998['apply'](_0xa15ac2,_0x50f937||[]))['next']());});};class Suggester{constructor(_0x25b876,_0x23313a,_0xadb0d3){this['input']=_0x23313a,this['options']=_0xadb0d3,this['suggestions']=_0x111029=>__awaiter$8(this,void 0x0,void 0x0,function*(){return new Promise((_0x4d97a2,_0x188cdb)=>__awaiter$8(this,void 0x0,void 0x0,function*(){try{let _0x137da1=_0x111029?0x0:this['options']['delay'];clearTimeout(this['filterTimer']),this['filterTimer']=setTimeout(()=>__awaiter$8(this,void 0x0,void 0x0,function*(){const _0x190911=yield this['getElements']();_0x4d97a2(_0x190911);return;}),_0x137da1);}catch(_0x80f82b){_0x188cdb(_0x80f82b);}}));}),this['hasMinimumCharacters']=_0x195ced=>{var _0x368128;!_0x195ced&&(_0x195ced='');var _0x2e0c5d=(_0x368128=this['options']['minimum_characters'])!==null&&_0x368128!==void 0x0?_0x368128:0x2;return _0x195ced['length']>=_0x2e0c5d;},this['getSuggestions']=_0x43f6fa=>{return new Promise((_0x203bda,_0x2f9500)=>__awaiter$8(this,void 0x0,void 0x0,function*(){var _0x3d9f4f,_0x39ccd3,_0x2cc36f;try{const _0x3ca1dc={'all':!![],'top':this['options']['suggestion_count'],'template':this['options']['suggestion_template']};this['options']['filter']&&(_0x3ca1dc['filter']=this['options']['filter']);this['options']['show_postcode']&&(_0x3ca1dc['show_postcode']=this['options']['show_postcode']);if(!_0x3ca1dc['location']){const _0x6ac424=Storage['list']();if(_0x6ac424['length']>0x0){const _0x1804cb=_0x6ac424[0x0];_0x3ca1dc['location']={'latitude':_0x1804cb['address']['latitude'],'longitude':_0x1804cb['address']['longitude']};}}const _0x247155=yield this['client']['autocomplete'](_0x43f6fa,_0x3ca1dc);if(!_0x247155['isSuccess']){const _0x95a9c=_0x247155['toFailed']();_0x2f9500(_0x95a9c),(_0x3d9f4f=this['input']['list'])===null||_0x3d9f4f===void 0x0?void 0x0:_0x3d9f4f['dispatchSuggestionsFailed'](_0x43f6fa,_0x95a9c['status'],_0x95a9c['message']);return;}const _0x2fc840=_0x247155['toSuccess']()['suggestions'];if(this['options']['highlight_search_text'])for(let _0xa7d0da of _0x2fc840){_0xa7d0da['address']=this['highlightSuggestion'](_0x43f6fa,_0xa7d0da['address']);}_0x203bda(_0x2fc840),(_0x39ccd3=this['input']['list'])===null||_0x39ccd3===void 0x0?void 0x0:_0x39ccd3['dispatchSuggestions'](_0x43f6fa,_0x2fc840);return;}catch(_0x33143b){_0x2f9500(_0x33143b),(_0x2cc36f=this['input']['list'])===null||_0x2cc36f===void 0x0?void 0x0:_0x2cc36f['dispatchSuggestionsFailed'](_0x43f6fa,0x190,_0x33143b);return;}}));},this['highlightSuggestion']=(_0x415b1c,_0x2d53fc)=>{const _0xd8f800='<mark>',_0x29e34b='</mark>';let _0x46f12c=_0x415b1c['trim']()['replace'](/ /g,',*\x20+');const _0x5503cd=new RegExp('\x5cb('+_0x46f12c+')','gi');return _0x2d53fc=_0x2d53fc['replace'](_0x5503cd,_0xd8f800+'$1'+_0x29e34b),_0x2d53fc;},this['client']=new Client(_0x25b876);}}

    var __awaiter$7=undefined&&undefined['__awaiter']||function(_0x574fa2,_0x1c5951,_0x88dd88,_0x41b823){function _0x30c73c(_0x4acae8){return _0x4acae8 instanceof _0x88dd88?_0x4acae8:new _0x88dd88(function(_0x597ef4){_0x597ef4(_0x4acae8);});}return new(_0x88dd88||(_0x88dd88=Promise))(function(_0x2f2f83,_0x1bf111){function _0x2fc9b9(_0x2ee782){try{_0x280823(_0x41b823['next'](_0x2ee782));}catch(_0x778285){_0x1bf111(_0x778285);}}function _0x385577(_0x1cc4e3){try{_0x280823(_0x41b823['throw'](_0x1cc4e3));}catch(_0x2f0ae4){_0x1bf111(_0x2f0ae4);}}function _0x280823(_0x31ca0b){_0x31ca0b['done']?_0x2f2f83(_0x31ca0b['value']):_0x30c73c(_0x31ca0b['value'])['then'](_0x2fc9b9,_0x385577);}_0x280823((_0x41b823=_0x41b823['apply'](_0x574fa2,_0x1c5951||[]))['next']());});};class AnchoredSuggester extends Suggester{constructor(_0xac3308,_0x584e82,_0x986673){super(_0xac3308,_0x584e82,_0x986673),this['getElements']=()=>__awaiter$7(this,void 0x0,void 0x0,function*(){const _0x33b78c=this['input']['textbox']['value'];if(this['options']['enable_history']&&!_0x33b78c){const _0x404570=Storage['list']();return _0x404570['map']((_0xff449e,_0x3bd0f7)=>{return new AnchoredHistoryContainer(this['input'],_0xff449e,this['options'],_0x3bd0f7)['element'];});}if(!this['hasMinimumCharacters'](_0x33b78c))return [];const _0x32eb28=yield this['getSuggestions'](_0x33b78c);return _0x32eb28['map']((_0x3b822f,_0x4dffa7)=>{return new AnchoredItemContainer(this['client'],this['input'],_0x3b822f,this['options'],_0x4dffa7)['element'];});});}}

    var __awaiter$6=undefined&&undefined['__awaiter']||function(_0x4f4275,_0x2fe62b,_0x1542de,_0x57506){function _0x22e680(_0x5c4947){return _0x5c4947 instanceof _0x1542de?_0x5c4947:new _0x1542de(function(_0x3cd47a){_0x3cd47a(_0x5c4947);});}return new(_0x1542de||(_0x1542de=Promise))(function(_0x215114,_0x5b5812){function _0x3ab0cb(_0x3f8bfa){try{_0x1ac100(_0x57506['next'](_0x3f8bfa));}catch(_0x1d3ee2){_0x5b5812(_0x1d3ee2);}}function _0x23e7ac(_0x55ec3c){try{_0x1ac100(_0x57506['throw'](_0x55ec3c));}catch(_0x1788e0){_0x5b5812(_0x1788e0);}}function _0x1ac100(_0x2f9dfd){_0x2f9dfd['done']?_0x215114(_0x2f9dfd['value']):_0x22e680(_0x2f9dfd['value'])['then'](_0x3ab0cb,_0x23e7ac);}_0x1ac100((_0x57506=_0x57506['apply'](_0x4f4275,_0x2fe62b||[]))['next']());});},_a$1;class Input{constructor(_0x5bc424,_0x1cbbb8,_0xf746e3){this['options']=_0x5bc424,this['textbox']=_0x1cbbb8,this['list']=_0xf746e3,this['repopulate']=!![],this['addEventHandlers']=()=>{this['textbox']['addEventListener']('paste',this['handlePaste']),this['textbox']['addEventListener']('keydown',this['handleKeyDown']),this['textbox']['addEventListener']('keyup',this['handleKeyUp']),this['textbox']['addEventListener']('focus',this['handleFocus']),this['textbox']['addEventListener']('focusout',this['handleFocusOut']);},this['destroy']=()=>{this['textbox']['removeAttribute']('aria-expanded'),this['textbox']['removeAttribute']('autocomplete'),this['textbox']['removeAttribute']('aria-autocomplete'),this['textbox']['removeAttribute']('role'),this['textbox']['removeAttribute']('aria-owns'),this['textbox']['removeEventListener']('paste',this['handlePaste']),this['textbox']['removeEventListener']('keydown',this['handleKeyDown']),this['textbox']['removeEventListener']('keyup',this['handleKeyUp']),this['textbox']['removeEventListener']('focus',this['handleFocus']),this['textbox']['removeEventListener']('focusout',this['handleFocusOut']);},this['value']=()=>{return this['textbox']['value'];},this['clear']=()=>{this['textbox']['value']='',this['populateList']();},this['handleKeyDown']=_0x10ae8b=>{switch(_0x10ae8b['key']){case'ArrowDown':this['handleDownKey'](_0x10ae8b);break;case'PageDown':this['handlePageDown'](_0x10ae8b);break;case'Escape':this['handleBlur'](_0x10ae8b);break;case'Enter':this['handleEnterKey'](_0x10ae8b);break;default:this['handleKeyDownDefault'](_0x10ae8b);break;}},this['handlePageDown']=_0x36bbbc=>{var _0xcfc132;this['list']&&this['list']['element']['children']['length']>0x0&&(this['list']['setSelectedIndex'](((_0xcfc132=this['list'])===null||_0xcfc132===void 0x0?void 0x0:_0xcfc132['element']['children']['length'])-0x1),_0x36bbbc['preventDefault']());},this['handleBlur']=_0x41f04f=>{this['clearList']();},this['handleEnterKey']=_0x46ab46=>{_0x46ab46['preventDefault']();},this['setValue']=_0x50ddfe=>{this['textbox']['value']=_0x50ddfe;},this['textbox']['setAttribute']('aria-expanded','false'),this['textbox']['setAttribute']('autocomplete','off'),this['textbox']['setAttribute']('aria-autocomplete','list'),this['textbox']['setAttribute']('role','combobox');}['clearList'](_0x280f49=0x64){clearTimeout(this['blurTimer']),this['blurTimer']=setTimeout(()=>{this['list']['clear']();},_0x280f49);}['setFocus'](){this['textbox']['focus']();}['dispatchSelected'](_0x17feaf,_0x3ddee5){this['list']['dispatchSelected'](_0x17feaf,this['textbox']['value'],_0x3ddee5);}['dispatchSelectedFailed'](_0x26a737,_0x11793e,_0x27bf0e){this['list']['dispatchSelectedFailed'](_0x26a737,_0x11793e,_0x27bf0e);}}_a$1=Input,Input['handleKeyUp']=(_0x3a3612,_0x50d720,_0x21913b)=>__awaiter$6(void 0x0,void 0x0,void 0x0,function*(){const _0x29f211=_0x3a3612['key']==='Backspace'||_0x3a3612['key']==='Delete';if(_0x29f211){const _0x1d41b2=yield _0x50d720['suggestions'](![]);_0x21913b['populate'](_0x1d41b2);}}),Input['handlePaste']=(_0x149a9b,_0x11fd0a,_0x471d02)=>__awaiter$6(void 0x0,void 0x0,void 0x0,function*(){const _0x19497a=yield _0x11fd0a['suggestions'](![]);_0x471d02['populate'](_0x19497a);}),Input['populateList']=(_0x56340b,_0xbb3ac1,_0x4b91bb)=>__awaiter$6(void 0x0,void 0x0,void 0x0,function*(){const _0x38eb85=yield _0x56340b['suggestions'](![]);_0xbb3ac1['populate'](_0x38eb85);}),Input['handleDownKey']=(_0x3e1382,_0x1d7e8d,_0x10d1aa)=>__awaiter$6(void 0x0,void 0x0,void 0x0,function*(){if(_0x1d7e8d['element']['children']['length']===0x0){const _0xd82c8c=yield _0x10d1aa['suggestions'](!![]);_0x1d7e8d['populate'](_0xd82c8c);}_0x1d7e8d['setSelectedIndex'](0x0),_0x3e1382['preventDefault']();}),Input['handleFocus']=(_0x45e825,_0x22c5f5,_0x42160a,_0x415c60)=>__awaiter$6(void 0x0,void 0x0,void 0x0,function*(){_0x22c5f5['resetSelectedIndex']();if(_0x415c60){const _0x3eb4ba=yield _0x42160a['suggestions'](!![]);_0x22c5f5['populate'](_0x3eb4ba);}else _0x415c60=!![];}),Input['handleKeyDownDefault']=(_0x378b6e,_0xe520a7,_0x19605d)=>__awaiter$6(void 0x0,void 0x0,void 0x0,function*(){if(_a$1['isPrintableKey'](_0x378b6e)){const _0x4babc5=yield _0x19605d['suggestions'](_0x378b6e['key']==='\x20');_0xe520a7['populate'](_0x4babc5);}}),Input['isPrintableKey']=_0x5f4d19=>{return _0x5f4d19['key']['length']===0x1||_0x5f4d19['key']==='Unidentified';};

    var __awaiter$5=undefined&&undefined['__awaiter']||function(_0x197c7b,_0x25d40c,_0x80e58c,_0x495e3c){function _0x2bd32d(_0x42e7b8){return _0x42e7b8 instanceof _0x80e58c?_0x42e7b8:new _0x80e58c(function(_0x3dec92){_0x3dec92(_0x42e7b8);});}return new(_0x80e58c||(_0x80e58c=Promise))(function(_0x554b05,_0x523fef){function _0x571fcb(_0x50b6ad){try{_0x37e3c5(_0x495e3c['next'](_0x50b6ad));}catch(_0x4a9946){_0x523fef(_0x4a9946);}}function _0x549c2b(_0x288034){try{_0x37e3c5(_0x495e3c['throw'](_0x288034));}catch(_0x4c1452){_0x523fef(_0x4c1452);}}function _0x37e3c5(_0x19c8d5){_0x19c8d5['done']?_0x554b05(_0x19c8d5['value']):_0x2bd32d(_0x19c8d5['value'])['then'](_0x571fcb,_0x549c2b);}_0x37e3c5((_0x495e3c=_0x495e3c['apply'](_0x197c7b,_0x25d40c||[]))['next']());});};class AnchoredInput extends Input{constructor(_0x37a3f2,_0x4154a4,_0x5f32c2,_0x57373b){super(_0x4154a4,_0x5f32c2,_0x57373b),this['options']=_0x4154a4,this['textbox']=_0x5f32c2,this['list']=_0x57373b,this['handleFocusOut']=_0x365a86=>{setTimeout(()=>{!this['list']['containsActiveElment']()&&this['list']['getSelectedIndex']()==-0x1&&this['clearList'](0x0);},0x32);},this['handleKeyUp']=_0x122b9d=>__awaiter$5(this,void 0x0,void 0x0,function*(){yield Input['handleKeyUp'](_0x122b9d,this['suggester'],this['list']);}),this['handlePaste']=_0x38205c=>__awaiter$5(this,void 0x0,void 0x0,function*(){Input['handlePaste'](_0x38205c,this['suggester'],this['list']);}),this['handleDownKey']=_0x485a4d=>__awaiter$5(this,void 0x0,void 0x0,function*(){yield Input['handleDownKey'](_0x485a4d,this['list'],this['suggester']);}),this['handleKeyDownDefault']=_0x18d43d=>__awaiter$5(this,void 0x0,void 0x0,function*(){Input['handleKeyDownDefault'](_0x18d43d,this['list'],this['suggester']);}),this['populateList']=()=>__awaiter$5(this,void 0x0,void 0x0,function*(){yield Input['populateList'](this['suggester'],this['list']);}),this['handleFocus']=_0x1ca0e0=>__awaiter$5(this,void 0x0,void 0x0,function*(){yield Input['handleFocus'](_0x1ca0e0,this['list'],this['suggester'],this['repopulate']);}),this['suggester']=new AnchoredSuggester(_0x37a3f2,this,_0x4154a4),this['addEventHandlers']();}}

    class AnchoredList extends List{constructor(_0xc5a8ae,_0x1cdbf0,_0x59a477,_0x49031f){super(_0x1cdbf0,_0x49031f),this['options']=_0x1cdbf0,this['textbox']=_0x59a477,this['instance']=_0x49031f,this['textboxCss']=(_0x494dba,_0x405904)=>{const _0x52a77f='\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20/*\x20textbox\x20*/\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20#'+_0x405904['id']+'{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20anchor-name:\x20--anchor-textbox-'+_0x494dba+';\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x20\x20\x20\x20\x20\x20\x20\x20';return _0x52a77f;},this['listCss']=(_0x17288f,_0x2f9290,_0xb6951b)=>{let _0x151f46='\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20/*\x20list\x20*/\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20#'+_0xb6951b+'{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20position-anchor:\x20--anchor-textbox-'+_0x17288f+';\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20top:anchor(bottom);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20left:anchor(left);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}';return _0x17288f===0x0&&(_0x151f46+='\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-list{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20position:absolute;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20visibility:hidden;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20border-style:var(--ga-autocomplete-list-border-style,solid);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20border-width:var(--ga-autocomplete-list-border-width,1px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20border-color:var(--ga-autocomplete-list-border-color,#ccc);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20z-index:\x20var(--ga-autocomplete-list-z-index,2011);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-top:var(--ga-autocomplete-list-padding-top,12px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-bottom:var(--ga-autocomplete-list-padding-bottom,12px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-left:var(--ga-autocomplete-list-padding-left,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-right:var(--ga-autocomplete-list-padding-right,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20box-shadow:\x20var(--ga-autocomplete-list-box-shadow,0\x202px\x204px\x20rgba(0,\x200,\x200,\x20.2));\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20background-color:\x20var(--ga-autocomplete-list-background-color,#fff);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-top:var(--ga-autocomplete-list-bottom-margin-top,2px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-right:var(--ga-autocomplete-list-bottom-margin-right,8px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-left:var(--ga-autocomplete-list-bottom-margin-left,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-bottom:var(--ga-autocomplete-list-bottom-margin-bottom,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20border-radius:var(--ga-autocomplete-list-border-radus,12px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-size:\x20var(--ga-autocomplete-list-font-size,0.9em);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x20var(--ga-autocomplete-list-width,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20min-width:\x20var(--ga-autocomplete-list-min-width,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x20\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-scroll{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20max-height:\x20var(--ga-autocomplete-list-max-height,30em);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20overflow-y:\x20var(--ga-autocomplete-list-overflow-y,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-list-item\x20.ga-autocomplete-icon\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding:2px\x205px\x205px\x200px;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-style:\x20normal;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-weight:\x20normal;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20display:\x20inline-block;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x202em;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20text-align:\x20center;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-variant:\x20normal;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20text-transform:\x20none;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20line-height:\x201em;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20-webkit-font-smoothing:\x20antialiased;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20-moz-osx-font-smoothing:\x20grayscale;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20position:\x20relative;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20top:\x200.1em;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-list-item\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-top:\x205px;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-bottom:\x2010px;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-left:\x2010px;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-right:\x2015px;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20'),_0x2f9290['enable_repositioning']&&(_0x151f46+='\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20#'+_0xb6951b+'{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20position-try-fallbacks:\x20--right\x20,--top,\x20--left;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20@position-try\x20--right\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20inset:auto;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20left:anchor(right);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20top:anchor(top);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin:auto;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-left:var(--ga-autocomplete-list-right-margin-left,6px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-right:var(--ga-autocomplete-list-right-margin-right,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-top:var(--ga-autocomplete-list-right-margin-top,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-bottom:var(--ga-autocomplete-list-right-margin-bottom,auto);\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:var(--ga-autocomplete-list-right-width,var(--ga-autocomplete-list-width));\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20min-width:\x20var(--ga-autocomplete-list-right-min-width,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20@position-try\x20--top\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20inset:auto;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20left:anchor(left);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20bottom:anchor(top);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin:auto;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-bottom:var(--ga-autocomplete-list-top-margin-bottom,6px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:var(--ga-autocomplete-list-top-width,var(--ga-autocomplete-list-width));\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20min-width:\x20var(--ga-autocomplete-list-top-min-width,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20@position-try\x20--left\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20inset:auto;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20right:anchor(left);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20top:anchor(top);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin:auto;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-right:var(--ga-autocomplete-list-left-margin-right,6px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:var(--ga-autocomplete-list-left-width,var(--ga-autocomplete-list-width));\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20min-width:\x20var(--ga-autocomplete-list-left-min-width,auto);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20}\x20'),_0x151f46;},this['input']=new AnchoredInput(_0xc5a8ae,_0x1cdbf0,_0x59a477,this),this['list']['classList']['add']('ga-autocomplete-anchored-list'),this['textbox']['insertAdjacentElement']('afterend',this['list']);_0x1cdbf0['list_style']&&this['list']['setAttribute']('style',_0x1cdbf0['list_style']);this['injectStyle']();if(_0x1cdbf0['full_length']){const _0x3fa4de=document['documentElement'];_0x3fa4de['style']['setProperty']('--ga-autocomplete-list-min-width',_0x59a477['offsetWidth']+'px');}}['setSelectedIndex'](_0x206919){List['setSelectedIndex'](_0x206919,this,this['input']);}['setFocus'](){List['setFocus'](this['selectedIndex'],this,this['input']);}['destroy'](){this['input']['destroy'](),super['destroy']();}['injectStyle'](){this['style']['appendChild'](document['createTextNode'](this['iconCss'](this['instance']))),this['style']['appendChild'](document['createTextNode'](this['listItemCss'](this['instance']))),this['style']['appendChild'](document['createTextNode'](this['textboxCss'](this['instance'],this['textbox']))),this['style']['appendChild'](document['createTextNode'](this['listCss'](this['instance'],this['options'],this['list']['id']))),this['list']['insertAdjacentElement']('beforebegin',this['style']);}}

    var ha = Object.defineProperty, fa = Object.defineProperties;
    var pa = Object.getOwnPropertyDescriptors;
    var Rr = Object.getOwnPropertySymbols;
    var da = Object.prototype.hasOwnProperty, ma = Object.prototype.propertyIsEnumerable;
    var Mr = (e, t, n) => t in e ? ha(e, t, { enumerable: !0, configurable: !0, writable: !0, value: n }) : e[t] = n, _ = (e, t) => {
      for (var n in t || (t = {}))
        da.call(t, n) && Mr(e, n, t[n]);
      if (Rr)
        for (var n of Rr(t))
          ma.call(t, n) && Mr(e, n, t[n]);
      return e;
    }, Z = (e, t) => fa(e, pa(t));
    var H = (e, t, n) => new Promise((r, i) => {
      var o = (l) => {
        try {
          c(n.next(l));
        } catch (a) {
          i(a);
        }
      }, s = (l) => {
        try {
          c(n.throw(l));
        } catch (a) {
          i(a);
        }
      }, c = (l) => l.done ? r(l.value) : Promise.resolve(l.value).then(o, s);
      c((n = n.apply(e, t)).next());
    });
    const Gn = Math.min, lt = Math.max, Zt = Math.round, Mt = Math.floor, $e = (e) => ({
      x: e,
      y: e
    });
    function ga(e, t) {
      return typeof e == "function" ? e(t) : e;
    }
    function ba(e) {
      return _({
        top: 0,
        right: 0,
        bottom: 0,
        left: 0
      }, e);
    }
    function ya(e) {
      return typeof e != "number" ? ba(e) : {
        top: e,
        right: e,
        bottom: e,
        left: e
      };
    }
    function Jt(e) {
      const {
        x: t,
        y: n,
        width: r,
        height: i
      } = e;
      return {
        width: r,
        height: i,
        top: n,
        left: t,
        right: t + r,
        bottom: n + i,
        x: t,
        y: n
      };
    }
    function ka(e, t) {
      return H(this, null, function* () {
        var n;
        t === void 0 && (t = {});
        const {
          x: r,
          y: i,
          platform: o,
          rects: s,
          elements: c,
          strategy: l
        } = e, {
          boundary: a = "clippingAncestors",
          rootBoundary: u = "viewport",
          elementContext: h = "floating",
          altBoundary: d = !1,
          padding: m = 0
        } = ga(t, e), w = ya(m), C = c[d ? h === "floating" ? "reference" : "floating" : h], b = Jt(yield o.getClippingRect({
          element: (n = yield o.isElement == null ? void 0 : o.isElement(C)) == null || n ? C : C.contextElement || (yield o.getDocumentElement == null ? void 0 : o.getDocumentElement(c.floating)),
          boundary: a,
          rootBoundary: u,
          strategy: l
        })), x = h === "floating" ? {
          x: r,
          y: i,
          width: s.floating.width,
          height: s.floating.height
        } : s.reference, T = yield o.getOffsetParent == null ? void 0 : o.getOffsetParent(c.floating), v = (yield o.isElement == null ? void 0 : o.isElement(T)) ? (yield o.getScale == null ? void 0 : o.getScale(T)) || {
          x: 1,
          y: 1
        } : {
          x: 1,
          y: 1
        }, A = Jt(o.convertOffsetParentRelativeRectToViewportRelativeRect ? yield o.convertOffsetParentRelativeRectToViewportRelativeRect({
          elements: c,
          rect: x,
          offsetParent: T,
          strategy: l
        }) : x);
        return {
          top: (b.top - A.top + w.top) / v.y,
          bottom: (A.bottom - b.bottom + w.bottom) / v.y,
          left: (b.left - A.left + w.left) / v.x,
          right: (A.right - b.right + w.right) / v.x
        };
      });
    }
    function hn() {
      return typeof window != "undefined";
    }
    function bt(e) {
      return to(e) ? (e.nodeName || "").toLowerCase() : "#document";
    }
    function fe(e) {
      var t;
      return (e == null || (t = e.ownerDocument) == null ? void 0 : t.defaultView) || window;
    }
    function Pe(e) {
      var t;
      return (t = (to(e) ? e.ownerDocument : e.document) || window.document) == null ? void 0 : t.documentElement;
    }
    function to(e) {
      return hn() ? e instanceof Node || e instanceof fe(e).Node : !1;
    }
    function we(e) {
      return hn() ? e instanceof Element || e instanceof fe(e).Element : !1;
    }
    function _e(e) {
      return hn() ? e instanceof HTMLElement || e instanceof fe(e).HTMLElement : !1;
    }
    function Nr(e) {
      return !hn() || typeof ShadowRoot == "undefined" ? !1 : e instanceof ShadowRoot || e instanceof fe(e).ShadowRoot;
    }
    function It(e) {
      const {
        overflow: t,
        overflowX: n,
        overflowY: r,
        display: i
      } = ve(e);
      return /auto|scroll|overlay|hidden|clip/.test(t + r + n) && !["inline", "contents"].includes(i);
    }
    function xa(e) {
      return ["table", "td", "th"].includes(bt(e));
    }
    function fn(e) {
      return [":popover-open", ":modal"].some((t) => {
        try {
          return e.matches(t);
        } catch (n) {
          return !1;
        }
      });
    }
    function ur(e) {
      const t = hr(), n = we(e) ? ve(e) : e;
      return n.transform !== "none" || n.perspective !== "none" || (n.containerType ? n.containerType !== "normal" : !1) || !t && (n.backdropFilter ? n.backdropFilter !== "none" : !1) || !t && (n.filter ? n.filter !== "none" : !1) || ["transform", "perspective", "filter"].some((r) => (n.willChange || "").includes(r)) || ["paint", "layout", "strict", "content"].some((r) => (n.contain || "").includes(r));
    }
    function wa(e) {
      let t = We(e);
      for (; _e(t) && !ft(t); ) {
        if (ur(t))
          return t;
        if (fn(t))
          return null;
        t = We(t);
      }
      return null;
    }
    function hr() {
      return typeof CSS == "undefined" || !CSS.supports ? !1 : CSS.supports("-webkit-backdrop-filter", "none");
    }
    function ft(e) {
      return ["html", "body", "#document"].includes(bt(e));
    }
    function ve(e) {
      return fe(e).getComputedStyle(e);
    }
    function pn(e) {
      return we(e) ? {
        scrollLeft: e.scrollLeft,
        scrollTop: e.scrollTop
      } : {
        scrollLeft: e.scrollX,
        scrollTop: e.scrollY
      };
    }
    function We(e) {
      if (bt(e) === "html")
        return e;
      const t = (
        // Step into the shadow DOM of the parent of a slotted node.
        e.assignedSlot || // DOM Element detected.
        e.parentNode || // ShadowRoot detected.
        Nr(e) && e.host || // Fallback.
        Pe(e)
      );
      return Nr(t) ? t.host : t;
    }
    function no(e) {
      const t = We(e);
      return ft(t) ? e.ownerDocument ? e.ownerDocument.body : e.body : _e(t) && It(t) ? t : no(t);
    }
    function Lt(e, t, n) {
      var r;
      t === void 0 && (t = []), n === void 0 && (n = !0);
      const i = no(e), o = i === ((r = e.ownerDocument) == null ? void 0 : r.body), s = fe(i);
      if (o) {
        const c = Vn(s);
        return t.concat(s, s.visualViewport || [], It(i) ? i : [], c && n ? Lt(c) : []);
      }
      return t.concat(i, Lt(i, [], n));
    }
    function Vn(e) {
      return e.parent && Object.getPrototypeOf(e.parent) ? e.frameElement : null;
    }
    function ro(e) {
      const t = ve(e);
      let n = parseFloat(t.width) || 0, r = parseFloat(t.height) || 0;
      const i = _e(e), o = i ? e.offsetWidth : n, s = i ? e.offsetHeight : r, c = Zt(n) !== o || Zt(r) !== s;
      return c && (n = o, r = s), {
        width: n,
        height: r,
        $: c
      };
    }
    function fr(e) {
      return we(e) ? e : e.contextElement;
    }
    function ct(e) {
      const t = fr(e);
      if (!_e(t))
        return $e(1);
      const n = t.getBoundingClientRect(), {
        width: r,
        height: i,
        $: o
      } = ro(t);
      let s = (o ? Zt(n.width) : n.width) / r, c = (o ? Zt(n.height) : n.height) / i;
      return (!s || !Number.isFinite(s)) && (s = 1), (!c || !Number.isFinite(c)) && (c = 1), {
        x: s,
        y: c
      };
    }
    const va = /* @__PURE__ */ $e(0);
    function io(e) {
      const t = fe(e);
      return !hr() || !t.visualViewport ? va : {
        x: t.visualViewport.offsetLeft,
        y: t.visualViewport.offsetTop
      };
    }
    function Sa(e, t, n) {
      return t === void 0 && (t = !1), !n || t && n !== fe(e) ? !1 : t;
    }
    function Qe(e, t, n, r) {
      t === void 0 && (t = !1), n === void 0 && (n = !1);
      const i = e.getBoundingClientRect(), o = fr(e);
      let s = $e(1);
      t && (r ? we(r) && (s = ct(r)) : s = ct(e));
      const c = Sa(o, n, r) ? io(o) : $e(0);
      let l = (i.left + c.x) / s.x, a = (i.top + c.y) / s.y, u = i.width / s.x, h = i.height / s.y;
      if (o) {
        const d = fe(o), m = r && we(r) ? fe(r) : r;
        let w = d, k = Vn(w);
        for (; k && r && m !== w; ) {
          const C = ct(k), b = k.getBoundingClientRect(), x = ve(k), T = b.left + (k.clientLeft + parseFloat(x.paddingLeft)) * C.x, v = b.top + (k.clientTop + parseFloat(x.paddingTop)) * C.y;
          l *= C.x, a *= C.y, u *= C.x, h *= C.y, l += T, a += v, w = fe(k), k = Vn(w);
        }
      }
      return Jt({
        width: u,
        height: h,
        x: l,
        y: a
      });
    }
    function pr(e, t) {
      const n = pn(e).scrollLeft;
      return t ? t.left + n : Qe(Pe(e)).left + n;
    }
    function oo(e, t, n) {
      n === void 0 && (n = !1);
      const r = e.getBoundingClientRect(), i = r.left + t.scrollLeft - (n ? 0 : (
        // RTL <body> scrollbar.
        pr(e, r)
      )), o = r.top + t.scrollTop;
      return {
        x: i,
        y: o
      };
    }
    function Ca(e) {
      let {
        elements: t,
        rect: n,
        offsetParent: r,
        strategy: i
      } = e;
      const o = i === "fixed", s = Pe(r), c = t ? fn(t.floating) : !1;
      if (r === s || c && o)
        return n;
      let l = {
        scrollLeft: 0,
        scrollTop: 0
      }, a = $e(1);
      const u = $e(0), h = _e(r);
      if ((h || !h && !o) && ((bt(r) !== "body" || It(s)) && (l = pn(r)), _e(r))) {
        const m = Qe(r);
        a = ct(r), u.x = m.x + r.clientLeft, u.y = m.y + r.clientTop;
      }
      const d = s && !h && !o ? oo(s, l, !0) : $e(0);
      return {
        width: n.width * a.x,
        height: n.height * a.y,
        x: n.x * a.x - l.scrollLeft * a.x + u.x + d.x,
        y: n.y * a.y - l.scrollTop * a.y + u.y + d.y
      };
    }
    function Ta(e) {
      return Array.from(e.getClientRects());
    }
    function Aa(e) {
      const t = Pe(e), n = pn(e), r = e.ownerDocument.body, i = lt(t.scrollWidth, t.clientWidth, r.scrollWidth, r.clientWidth), o = lt(t.scrollHeight, t.clientHeight, r.scrollHeight, r.clientHeight);
      let s = -n.scrollLeft + pr(e);
      const c = -n.scrollTop;
      return ve(r).direction === "rtl" && (s += lt(t.clientWidth, r.clientWidth) - i), {
        width: i,
        height: o,
        x: s,
        y: c
      };
    }
    function Oa(e, t) {
      const n = fe(e), r = Pe(e), i = n.visualViewport;
      let o = r.clientWidth, s = r.clientHeight, c = 0, l = 0;
      if (i) {
        o = i.width, s = i.height;
        const a = hr();
        (!a || a && t === "fixed") && (c = i.offsetLeft, l = i.offsetTop);
      }
      return {
        width: o,
        height: s,
        x: c,
        y: l
      };
    }
    function Ea(e, t) {
      const n = Qe(e, !0, t === "fixed"), r = n.top + e.clientTop, i = n.left + e.clientLeft, o = _e(e) ? ct(e) : $e(1), s = e.clientWidth * o.x, c = e.clientHeight * o.y, l = i * o.x, a = r * o.y;
      return {
        width: s,
        height: c,
        x: l,
        y: a
      };
    }
    function Dr(e, t, n) {
      let r;
      if (t === "viewport")
        r = Oa(e, n);
      else if (t === "document")
        r = Aa(Pe(e));
      else if (we(t))
        r = Ea(t, n);
      else {
        const i = io(e);
        r = {
          x: t.x - i.x,
          y: t.y - i.y,
          width: t.width,
          height: t.height
        };
      }
      return Jt(r);
    }
    function so(e, t) {
      const n = We(e);
      return n === t || !we(n) || ft(n) ? !1 : ve(n).position === "fixed" || so(n, t);
    }
    function La(e, t) {
      const n = t.get(e);
      if (n)
        return n;
      let r = Lt(e, [], !1).filter((c) => we(c) && bt(c) !== "body"), i = null;
      const o = ve(e).position === "fixed";
      let s = o ? We(e) : e;
      for (; we(s) && !ft(s); ) {
        const c = ve(s), l = ur(s);
        !l && c.position === "fixed" && (i = null), (o ? !l && !i : !l && c.position === "static" && !!i && ["absolute", "fixed"].includes(i.position) || It(s) && !l && so(e, s)) ? r = r.filter((u) => u !== s) : i = c, s = We(s);
      }
      return t.set(e, r), r;
    }
    function $a(e) {
      let {
        element: t,
        boundary: n,
        rootBoundary: r,
        strategy: i
      } = e;
      const s = [...n === "clippingAncestors" ? fn(t) ? [] : La(t, this._c) : [].concat(n), r], c = s[0], l = s.reduce((a, u) => {
        const h = Dr(t, u, i);
        return a.top = lt(h.top, a.top), a.right = Gn(h.right, a.right), a.bottom = Gn(h.bottom, a.bottom), a.left = lt(h.left, a.left), a;
      }, Dr(t, c, i));
      return {
        width: l.right - l.left,
        height: l.bottom - l.top,
        x: l.left,
        y: l.top
      };
    }
    function _a(e) {
      const {
        width: t,
        height: n
      } = ro(e);
      return {
        width: t,
        height: n
      };
    }
    function Pa(e, t, n) {
      const r = _e(t), i = Pe(t), o = n === "fixed", s = Qe(e, !0, o, t);
      let c = {
        scrollLeft: 0,
        scrollTop: 0
      };
      const l = $e(0);
      if (r || !r && !o)
        if ((bt(t) !== "body" || It(i)) && (c = pn(t)), r) {
          const d = Qe(t, !0, o, t);
          l.x = d.x + t.clientLeft, l.y = d.y + t.clientTop;
        } else i && (l.x = pr(i));
      const a = i && !r && !o ? oo(i, c) : $e(0), u = s.left + c.scrollLeft - l.x - a.x, h = s.top + c.scrollTop - l.y - a.y;
      return {
        x: u,
        y: h,
        width: s.width,
        height: s.height
      };
    }
    function Sn(e) {
      return ve(e).position === "static";
    }
    function jr(e, t) {
      if (!_e(e) || ve(e).position === "fixed")
        return null;
      if (t)
        return t(e);
      let n = e.offsetParent;
      return Pe(e) === n && (n = n.ownerDocument.body), n;
    }
    function ao(e, t) {
      const n = fe(e);
      if (fn(e))
        return n;
      if (!_e(e)) {
        let i = We(e);
        for (; i && !ft(i); ) {
          if (we(i) && !Sn(i))
            return i;
          i = We(i);
        }
        return n;
      }
      let r = jr(e, t);
      for (; r && xa(r) && Sn(r); )
        r = jr(r, t);
      return r && ft(r) && Sn(r) && !ur(r) ? n : r || wa(e) || n;
    }
    const za = function(e) {
      return H(this, null, function* () {
        const t = this.getOffsetParent || ao, n = this.getDimensions, r = yield n(e.floating);
        return {
          reference: Pa(e.reference, yield t(e.floating), e.strategy),
          floating: {
            x: 0,
            y: 0,
            width: r.width,
            height: r.height
          }
        };
      });
    };
    function Ia(e) {
      return ve(e).direction === "rtl";
    }
    const ee = {
      convertOffsetParentRelativeRectToViewportRelativeRect: Ca,
      getDocumentElement: Pe,
      getClippingRect: $a,
      getOffsetParent: ao,
      getElementRects: za,
      getClientRects: Ta,
      getDimensions: _a,
      getScale: ct,
      isElement: we,
      isRTL: Ia
    };
    function Ra(e, t) {
      let n = null, r;
      const i = Pe(e);
      function o() {
        var c;
        clearTimeout(r), (c = n) == null || c.disconnect(), n = null;
      }
      function s(c, l) {
        c === void 0 && (c = !1), l === void 0 && (l = 1), o();
        const {
          left: a,
          top: u,
          width: h,
          height: d
        } = e.getBoundingClientRect();
        if (c || t(), !h || !d)
          return;
        const m = Mt(u), w = Mt(i.clientWidth - (a + h)), k = Mt(i.clientHeight - (u + d)), C = Mt(a), x = {
          rootMargin: -m + "px " + -w + "px " + -k + "px " + -C + "px",
          threshold: lt(0, Gn(1, l)) || 1
        };
        let T = !0;
        function v(A) {
          const P = A[0].intersectionRatio;
          if (P !== l) {
            if (!T)
              return s();
            P ? s(!1, P) : r = setTimeout(() => {
              s(!1, 1e-7);
            }, 1e3);
          }
          T = !1;
        }
        try {
          n = new IntersectionObserver(v, Z(_({}, x), {
            // Handle <iframe>s
            root: i.ownerDocument
          }));
        } catch (A) {
          n = new IntersectionObserver(v, x);
        }
        n.observe(e);
      }
      return s(!0), o;
    }
    function lo(e, t, n, r) {
      r === void 0 && (r = {});
      const {
        ancestorScroll: i = !0,
        ancestorResize: o = !0,
        elementResize: s = typeof ResizeObserver == "function",
        layoutShift: c = typeof IntersectionObserver == "function",
        animationFrame: l = !1
      } = r, a = fr(e), u = i || o ? [...a ? Lt(a) : [], ...Lt(t)] : [];
      u.forEach((b) => {
        i && b.addEventListener("scroll", n, {
          passive: !0
        }), o && b.addEventListener("resize", n);
      });
      const h = a && c ? Ra(a, n) : null;
      let d = -1, m = null;
      s && (m = new ResizeObserver((b) => {
        let [x] = b;
        x && x.target === a && m && (m.unobserve(t), cancelAnimationFrame(d), d = requestAnimationFrame(() => {
          var T;
          (T = m) == null || T.observe(t);
        })), n();
      }), a && !l && m.observe(a), m.observe(t));
      let w, k = l ? Qe(e) : null;
      l && C();
      function C() {
        const b = Qe(e);
        k && (b.x !== k.x || b.y !== k.y || b.width !== k.width || b.height !== k.height) && n(), k = b, w = requestAnimationFrame(C);
      }
      return n(), () => {
        var b;
        u.forEach((x) => {
          i && x.removeEventListener("scroll", n), o && x.removeEventListener("resize", n);
        }), h == null || h(), (b = m) == null || b.disconnect(), m = null, l && cancelAnimationFrame(w);
      };
    }
    const Ma = ka, De = 0, y = 1, $ = 2, G = 3, F = 4, Te = 5, dn = 6, te = 7, se = 8, I = 9, L = 10, B = 11, z = 12, W = 13, Rt = 14, ae = 15, ne = 16, re = 17, ce = 18, pe = 19, Se = 20, M = 21, E = 22, V = 23, ue = 24, X = 25, Na = 0;
    function Q(e) {
      return e >= 48 && e <= 57;
    }
    function He(e) {
      return Q(e) || // 0 .. 9
      e >= 65 && e <= 70 || // A .. F
      e >= 97 && e <= 102;
    }
    function dr(e) {
      return e >= 65 && e <= 90;
    }
    function Da(e) {
      return e >= 97 && e <= 122;
    }
    function ja(e) {
      return dr(e) || Da(e);
    }
    function Fa(e) {
      return e >= 128;
    }
    function en(e) {
      return ja(e) || Fa(e) || e === 95;
    }
    function co(e) {
      return en(e) || Q(e) || e === 45;
    }
    function Ba(e) {
      return e >= 0 && e <= 8 || e === 11 || e >= 14 && e <= 31 || e === 127;
    }
    function tn(e) {
      return e === 10 || e === 13 || e === 12;
    }
    function Xe(e) {
      return tn(e) || e === 32 || e === 9;
    }
    function Le(e, t) {
      return !(e !== 92 || tn(t) || t === Na);
    }
    function qt(e, t, n) {
      return e === 45 ? en(t) || t === 45 || Le(t, n) : en(e) ? !0 : e === 92 ? Le(e, t) : !1;
    }
    function Cn(e, t, n) {
      return e === 43 || e === 45 ? Q(t) ? 2 : t === 46 && Q(n) ? 3 : 0 : e === 46 ? Q(t) ? 2 : 0 : Q(e) ? 1 : 0;
    }
    function uo(e) {
      return e === 65279 || e === 65534 ? 1 : 0;
    }
    const Kn = new Array(128), Wa = 128, Gt = 130, ho = 131, mr = 132, fo = 133;
    for (let e = 0; e < Kn.length; e++)
      Kn[e] = Xe(e) && Gt || Q(e) && ho || en(e) && mr || Ba(e) && fo || e || Wa;
    function Tn(e) {
      return e < 128 ? Kn[e] : mr;
    }
    function ut(e, t) {
      return t < e.length ? e.charCodeAt(t) : 0;
    }
    function Yn(e, t, n) {
      return n === 13 && ut(e, t + 1) === 10 ? 2 : 1;
    }
    function ht(e, t, n) {
      let r = e.charCodeAt(t);
      return dr(r) && (r = r | 32), r === n;
    }
    function $t(e, t, n, r) {
      if (n - t !== r.length || t < 0 || n > e.length)
        return !1;
      for (let i = t; i < n; i++) {
        const o = r.charCodeAt(i - t);
        let s = e.charCodeAt(i);
        if (dr(s) && (s = s | 32), s !== o)
          return !1;
      }
      return !0;
    }
    function Ha(e, t) {
      for (; t >= 0 && Xe(e.charCodeAt(t)); t--)
        ;
      return t + 1;
    }
    function Nt(e, t) {
      for (; t < e.length && Xe(e.charCodeAt(t)); t++)
        ;
      return t;
    }
    function An(e, t) {
      for (; t < e.length && Q(e.charCodeAt(t)); t++)
        ;
      return t;
    }
    function pt(e, t) {
      if (t += 2, He(ut(e, t - 1))) {
        for (const r = Math.min(e.length, t + 5); t < r && He(ut(e, t)); t++)
          ;
        const n = ut(e, t);
        Xe(n) && (t += Yn(e, t, n));
      }
      return t;
    }
    function Dt(e, t) {
      for (; t < e.length; t++) {
        const n = e.charCodeAt(t);
        if (!co(n)) {
          if (Le(n, ut(e, t + 1))) {
            t = pt(e, t) - 1;
            continue;
          }
          break;
        }
      }
      return t;
    }
    function mn(e, t) {
      let n = e.charCodeAt(t);
      if ((n === 43 || n === 45) && (n = e.charCodeAt(t += 1)), Q(n) && (t = An(e, t + 1), n = e.charCodeAt(t)), n === 46 && Q(e.charCodeAt(t + 1)) && (t += 2, t = An(e, t)), ht(
        e,
        t,
        101
        /* e */
      )) {
        let r = 0;
        n = e.charCodeAt(t + 1), (n === 45 || n === 43) && (r = 1, n = e.charCodeAt(t + 2)), Q(n) && (t = An(e, t + 1 + r + 1));
      }
      return t;
    }
    function On(e, t) {
      for (; t < e.length; t++) {
        const n = e.charCodeAt(t);
        if (n === 41) {
          t++;
          break;
        }
        Le(n, ut(e, t + 1)) && (t = pt(e, t));
      }
      return t;
    }
    function po(e) {
      if (e.length === 1 && !He(e.charCodeAt(0)))
        return e[0];
      let t = parseInt(e, 16);
      return (t === 0 || // If this number is zero,
      t >= 55296 && t <= 57343 || // or is for a surrogate,
      t > 1114111) && (t = 65533), String.fromCodePoint(t);
    }
    const mo = [
      "EOF-token",
      "ident-token",
      "function-token",
      "at-keyword-token",
      "hash-token",
      "string-token",
      "bad-string-token",
      "url-token",
      "bad-url-token",
      "delim-token",
      "number-token",
      "percentage-token",
      "dimension-token",
      "whitespace-token",
      "CDO-token",
      "CDC-token",
      "colon-token",
      "semicolon-token",
      "comma-token",
      "[-token",
      "]-token",
      "(-token",
      ")-token",
      "{-token",
      "}-token",
      "comment-token"
    ], Ua = 16 * 1024;
    function nn(e = null, t) {
      return e === null || e.length < t ? new Uint32Array(Math.max(t + 1024, Ua)) : e;
    }
    const Fr = 10, qa = 12, Br = 13;
    function Wr(e) {
      const t = e.source, n = t.length, r = t.length > 0 ? uo(t.charCodeAt(0)) : 0, i = nn(e.lines, n), o = nn(e.columns, n);
      let s = e.startLine, c = e.startColumn;
      for (let l = r; l < n; l++) {
        const a = t.charCodeAt(l);
        i[l] = s, o[l] = c++, (a === Fr || a === Br || a === qa) && (a === Br && l + 1 < n && t.charCodeAt(l + 1) === Fr && (l++, i[l] = s, o[l] = c), s++, c = 1);
      }
      i[n] = s, o[n] = c, e.lines = i, e.columns = o, e.computed = !0;
    }
    class Ga {
      constructor() {
        this.lines = null, this.columns = null, this.computed = !1;
      }
      setSource(t, n = 0, r = 1, i = 1) {
        this.source = t, this.startOffset = n, this.startLine = r, this.startColumn = i, this.computed = !1;
      }
      getLocation(t, n) {
        return this.computed || Wr(this), {
          source: n,
          offset: this.startOffset + t,
          line: this.lines[t],
          column: this.columns[t]
        };
      }
      getLocationRange(t, n, r) {
        return this.computed || Wr(this), {
          source: r,
          start: {
            offset: this.startOffset + t,
            line: this.lines[t],
            column: this.columns[t]
          },
          end: {
            offset: this.startOffset + n,
            line: this.lines[n],
            column: this.columns[n]
          }
        };
      }
    }
    const de = 16777215, Ae = 24, Va = /* @__PURE__ */ new Map([
      [$, E],
      [M, E],
      [pe, Se],
      [V, ue]
    ]);
    class Ka {
      constructor(t, n) {
        this.setSource(t, n);
      }
      reset() {
        this.eof = !1, this.tokenIndex = -1, this.tokenType = 0, this.tokenStart = this.firstCharOffset, this.tokenEnd = this.firstCharOffset;
      }
      setSource(t = "", n = () => {
      }) {
        t = String(t || "");
        const r = t.length, i = nn(this.offsetAndType, t.length + 1), o = nn(this.balance, t.length + 1);
        let s = 0, c = 0, l = 0, a = -1;
        for (this.offsetAndType = null, this.balance = null, n(t, (u, h, d) => {
          switch (u) {
            default:
              o[s] = r;
              break;
            case c: {
              let m = l & de;
              for (l = o[m], c = l >> Ae, o[s] = m, o[m++] = s; m < s; m++)
                o[m] === r && (o[m] = s);
              break;
            }
            case M:
            case $:
            case pe:
            case V:
              o[s] = l, c = Va.get(u), l = c << Ae | s;
              break;
          }
          i[s++] = u << Ae | d, a === -1 && (a = h);
        }), i[s] = De << Ae | r, o[s] = r, o[r] = r; l !== 0; ) {
          const u = l & de;
          l = o[u], o[u] = r;
        }
        this.source = t, this.firstCharOffset = a === -1 ? 0 : a, this.tokenCount = s, this.offsetAndType = i, this.balance = o, this.reset(), this.next();
      }
      lookupType(t) {
        return t += this.tokenIndex, t < this.tokenCount ? this.offsetAndType[t] >> Ae : De;
      }
      lookupTypeNonSC(t) {
        for (let n = this.tokenIndex; n < this.tokenCount; n++) {
          const r = this.offsetAndType[n] >> Ae;
          if (r !== W && r !== X && t-- === 0)
            return r;
        }
        return De;
      }
      lookupOffset(t) {
        return t += this.tokenIndex, t < this.tokenCount ? this.offsetAndType[t - 1] & de : this.source.length;
      }
      lookupOffsetNonSC(t) {
        for (let n = this.tokenIndex; n < this.tokenCount; n++) {
          const r = this.offsetAndType[n] >> Ae;
          if (r !== W && r !== X && t-- === 0)
            return n - this.tokenIndex;
        }
        return De;
      }
      lookupValue(t, n) {
        return t += this.tokenIndex, t < this.tokenCount ? $t(
          this.source,
          this.offsetAndType[t - 1] & de,
          this.offsetAndType[t] & de,
          n
        ) : !1;
      }
      getTokenStart(t) {
        return t === this.tokenIndex ? this.tokenStart : t > 0 ? t < this.tokenCount ? this.offsetAndType[t - 1] & de : this.offsetAndType[this.tokenCount] & de : this.firstCharOffset;
      }
      substrToCursor(t) {
        return this.source.substring(t, this.tokenStart);
      }
      isBalanceEdge(t) {
        return this.balance[this.tokenIndex] < t;
      }
      isDelim(t, n) {
        return n ? this.lookupType(n) === I && this.source.charCodeAt(this.lookupOffset(n)) === t : this.tokenType === I && this.source.charCodeAt(this.tokenStart) === t;
      }
      skip(t) {
        let n = this.tokenIndex + t;
        n < this.tokenCount ? (this.tokenIndex = n, this.tokenStart = this.offsetAndType[n - 1] & de, n = this.offsetAndType[n], this.tokenType = n >> Ae, this.tokenEnd = n & de) : (this.tokenIndex = this.tokenCount, this.next());
      }
      next() {
        let t = this.tokenIndex + 1;
        t < this.tokenCount ? (this.tokenIndex = t, this.tokenStart = this.tokenEnd, t = this.offsetAndType[t], this.tokenType = t >> Ae, this.tokenEnd = t & de) : (this.eof = !0, this.tokenIndex = this.tokenCount, this.tokenType = De, this.tokenStart = this.tokenEnd = this.source.length);
      }
      skipSC() {
        for (; this.tokenType === W || this.tokenType === X; )
          this.next();
      }
      skipUntilBalanced(t, n) {
        let r = t, i, o;
        e:
          for (; r < this.tokenCount; r++) {
            if (i = this.balance[r], i < t)
              break e;
            switch (o = r > 0 ? this.offsetAndType[r - 1] & de : this.firstCharOffset, n(this.source.charCodeAt(o))) {
              case 1:
                break e;
              case 2:
                r++;
                break e;
              default:
                this.balance[i] === r && (r = i);
            }
          }
        this.skip(r - this.tokenIndex);
      }
      forEachToken(t) {
        for (let n = 0, r = this.firstCharOffset; n < this.tokenCount; n++) {
          const i = r, o = this.offsetAndType[n], s = o & de, c = o >> Ae;
          r = s, t(c, i, s, n);
        }
      }
      dump() {
        const t = new Array(this.tokenCount);
        return this.forEachToken((n, r, i, o) => {
          t[o] = {
            idx: o,
            type: mo[n],
            chunk: this.source.substring(r, i),
            balance: this.balance[o]
          };
        }), t;
      }
    }
    function gn(e, t) {
      function n(h) {
        return h < c ? e.charCodeAt(h) : 0;
      }
      function r() {
        if (a = mn(e, a), qt(n(a), n(a + 1), n(a + 2))) {
          u = z, a = Dt(e, a);
          return;
        }
        if (n(a) === 37) {
          u = B, a++;
          return;
        }
        u = L;
      }
      function i() {
        const h = a;
        if (a = Dt(e, a), $t(e, h, a, "url") && n(a) === 40) {
          if (a = Nt(e, a + 1), n(a) === 34 || n(a) === 39) {
            u = $, a = h + 4;
            return;
          }
          s();
          return;
        }
        if (n(a) === 40) {
          u = $, a++;
          return;
        }
        u = y;
      }
      function o(h) {
        for (h || (h = n(a++)), u = Te; a < e.length; a++) {
          const d = e.charCodeAt(a);
          switch (Tn(d)) {
            case h:
              a++;
              return;
            case Gt:
              if (tn(d)) {
                a += Yn(e, a, d), u = dn;
                return;
              }
              break;
            case 92:
              if (a === e.length - 1)
                break;
              const m = n(a + 1);
              tn(m) ? a += Yn(e, a + 1, m) : Le(d, m) && (a = pt(e, a) - 1);
              break;
          }
        }
      }
      function s() {
        for (u = te, a = Nt(e, a); a < e.length; a++) {
          const h = e.charCodeAt(a);
          switch (Tn(h)) {
            case 41:
              a++;
              return;
            case Gt:
              if (a = Nt(e, a), n(a) === 41 || a >= e.length) {
                a < e.length && a++;
                return;
              }
              a = On(e, a), u = se;
              return;
            case 34:
            case 39:
            case 40:
            case fo:
              a = On(e, a), u = se;
              return;
            case 92:
              if (Le(h, n(a + 1))) {
                a = pt(e, a) - 1;
                break;
              }
              a = On(e, a), u = se;
              return;
          }
        }
      }
      e = String(e || "");
      const c = e.length;
      let l = uo(n(0)), a = l, u;
      for (; a < c; ) {
        const h = e.charCodeAt(a);
        switch (Tn(h)) {
          case Gt:
            u = W, a = Nt(e, a + 1);
            break;
          case 34:
            o();
            break;
          case 35:
            co(n(a + 1)) || Le(n(a + 1), n(a + 2)) ? (u = F, a = Dt(e, a + 1)) : (u = I, a++);
            break;
          case 39:
            o();
            break;
          case 40:
            u = M, a++;
            break;
          case 41:
            u = E, a++;
            break;
          case 43:
            Cn(h, n(a + 1), n(a + 2)) ? r() : (u = I, a++);
            break;
          case 44:
            u = ce, a++;
            break;
          case 45:
            Cn(h, n(a + 1), n(a + 2)) ? r() : n(a + 1) === 45 && n(a + 2) === 62 ? (u = ae, a = a + 3) : qt(h, n(a + 1), n(a + 2)) ? i() : (u = I, a++);
            break;
          case 46:
            Cn(h, n(a + 1), n(a + 2)) ? r() : (u = I, a++);
            break;
          case 47:
            n(a + 1) === 42 ? (u = X, a = e.indexOf("*/", a + 2), a = a === -1 ? e.length : a + 2) : (u = I, a++);
            break;
          case 58:
            u = ne, a++;
            break;
          case 59:
            u = re, a++;
            break;
          case 60:
            n(a + 1) === 33 && n(a + 2) === 45 && n(a + 3) === 45 ? (u = Rt, a = a + 4) : (u = I, a++);
            break;
          case 64:
            qt(n(a + 1), n(a + 2), n(a + 3)) ? (u = G, a = Dt(e, a + 1)) : (u = I, a++);
            break;
          case 91:
            u = pe, a++;
            break;
          case 92:
            Le(h, n(a + 1)) ? i() : (u = I, a++);
            break;
          case 93:
            u = Se, a++;
            break;
          case 123:
            u = V, a++;
            break;
          case 125:
            u = ue, a++;
            break;
          case ho:
            r();
            break;
          case mr:
            i();
            break;
          default:
            u = I, a++;
        }
        t(u, l, l = a);
      }
    }
    let nt = null;
    class q {
      static createItem(t) {
        return {
          prev: null,
          next: null,
          data: t
        };
      }
      constructor() {
        this.head = null, this.tail = null, this.cursor = null;
      }
      createItem(t) {
        return q.createItem(t);
      }
      // cursor helpers
      allocateCursor(t, n) {
        let r;
        return nt !== null ? (r = nt, nt = nt.cursor, r.prev = t, r.next = n, r.cursor = this.cursor) : r = {
          prev: t,
          next: n,
          cursor: this.cursor
        }, this.cursor = r, r;
      }
      releaseCursor() {
        const { cursor: t } = this;
        this.cursor = t.cursor, t.prev = null, t.next = null, t.cursor = nt, nt = t;
      }
      updateCursors(t, n, r, i) {
        let { cursor: o } = this;
        for (; o !== null; )
          o.prev === t && (o.prev = n), o.next === r && (o.next = i), o = o.cursor;
      }
      *[Symbol.iterator]() {
        for (let t = this.head; t !== null; t = t.next)
          yield t.data;
      }
      // getters
      get size() {
        let t = 0;
        for (let n = this.head; n !== null; n = n.next)
          t++;
        return t;
      }
      get isEmpty() {
        return this.head === null;
      }
      get first() {
        return this.head && this.head.data;
      }
      get last() {
        return this.tail && this.tail.data;
      }
      // convertors
      fromArray(t) {
        let n = null;
        this.head = null;
        for (let r of t) {
          const i = q.createItem(r);
          n !== null ? n.next = i : this.head = i, i.prev = n, n = i;
        }
        return this.tail = n, this;
      }
      toArray() {
        return [...this];
      }
      toJSON() {
        return [...this];
      }
      // array-like methods
      forEach(t, n = this) {
        const r = this.allocateCursor(null, this.head);
        for (; r.next !== null; ) {
          const i = r.next;
          r.next = i.next, t.call(n, i.data, i, this);
        }
        this.releaseCursor();
      }
      forEachRight(t, n = this) {
        const r = this.allocateCursor(this.tail, null);
        for (; r.prev !== null; ) {
          const i = r.prev;
          r.prev = i.prev, t.call(n, i.data, i, this);
        }
        this.releaseCursor();
      }
      reduce(t, n, r = this) {
        let i = this.allocateCursor(null, this.head), o = n, s;
        for (; i.next !== null; )
          s = i.next, i.next = s.next, o = t.call(r, o, s.data, s, this);
        return this.releaseCursor(), o;
      }
      reduceRight(t, n, r = this) {
        let i = this.allocateCursor(this.tail, null), o = n, s;
        for (; i.prev !== null; )
          s = i.prev, i.prev = s.prev, o = t.call(r, o, s.data, s, this);
        return this.releaseCursor(), o;
      }
      some(t, n = this) {
        for (let r = this.head; r !== null; r = r.next)
          if (t.call(n, r.data, r, this))
            return !0;
        return !1;
      }
      map(t, n = this) {
        const r = new q();
        for (let i = this.head; i !== null; i = i.next)
          r.appendData(t.call(n, i.data, i, this));
        return r;
      }
      filter(t, n = this) {
        const r = new q();
        for (let i = this.head; i !== null; i = i.next)
          t.call(n, i.data, i, this) && r.appendData(i.data);
        return r;
      }
      nextUntil(t, n, r = this) {
        if (t === null)
          return;
        const i = this.allocateCursor(null, t);
        for (; i.next !== null; ) {
          const o = i.next;
          if (i.next = o.next, n.call(r, o.data, o, this))
            break;
        }
        this.releaseCursor();
      }
      prevUntil(t, n, r = this) {
        if (t === null)
          return;
        const i = this.allocateCursor(t, null);
        for (; i.prev !== null; ) {
          const o = i.prev;
          if (i.prev = o.prev, n.call(r, o.data, o, this))
            break;
        }
        this.releaseCursor();
      }
      // mutation
      clear() {
        this.head = null, this.tail = null;
      }
      copy() {
        const t = new q();
        for (let n of this)
          t.appendData(n);
        return t;
      }
      prepend(t) {
        return this.updateCursors(null, t, this.head, t), this.head !== null ? (this.head.prev = t, t.next = this.head) : this.tail = t, this.head = t, this;
      }
      prependData(t) {
        return this.prepend(q.createItem(t));
      }
      append(t) {
        return this.insert(t);
      }
      appendData(t) {
        return this.insert(q.createItem(t));
      }
      insert(t, n = null) {
        if (n !== null)
          if (this.updateCursors(n.prev, t, n, t), n.prev === null) {
            if (this.head !== n)
              throw new Error("before doesn't belong to list");
            this.head = t, n.prev = t, t.next = n, this.updateCursors(null, t);
          } else
            n.prev.next = t, t.prev = n.prev, n.prev = t, t.next = n;
        else
          this.updateCursors(this.tail, t, null, t), this.tail !== null ? (this.tail.next = t, t.prev = this.tail) : this.head = t, this.tail = t;
        return this;
      }
      insertData(t, n) {
        return this.insert(q.createItem(t), n);
      }
      remove(t) {
        if (this.updateCursors(t, t.prev, t, t.next), t.prev !== null)
          t.prev.next = t.next;
        else {
          if (this.head !== t)
            throw new Error("item doesn't belong to list");
          this.head = t.next;
        }
        if (t.next !== null)
          t.next.prev = t.prev;
        else {
          if (this.tail !== t)
            throw new Error("item doesn't belong to list");
          this.tail = t.prev;
        }
        return t.prev = null, t.next = null, t;
      }
      push(t) {
        this.insert(q.createItem(t));
      }
      pop() {
        return this.tail !== null ? this.remove(this.tail) : null;
      }
      unshift(t) {
        this.prepend(q.createItem(t));
      }
      shift() {
        return this.head !== null ? this.remove(this.head) : null;
      }
      prependList(t) {
        return this.insertList(t, this.head);
      }
      appendList(t) {
        return this.insertList(t);
      }
      insertList(t, n) {
        return t.head === null ? this : (n != null ? (this.updateCursors(n.prev, t.tail, n, t.head), n.prev !== null ? (n.prev.next = t.head, t.head.prev = n.prev) : this.head = t.head, n.prev = t.tail, t.tail.next = n) : (this.updateCursors(this.tail, t.tail, null, t.head), this.tail !== null ? (this.tail.next = t.head, t.head.prev = this.tail) : this.head = t.head, this.tail = t.tail), t.head = null, t.tail = null, this);
      }
      replace(t, n) {
        "head" in n ? this.insertList(n, t) : this.insert(n, t), this.remove(t);
      }
    }
    function bn(e, t) {
      const n = Object.create(SyntaxError.prototype), r = new Error();
      return Object.assign(n, {
        name: e,
        message: t,
        get stack() {
          return (r.stack || "").replace(/^(.+\n){1,3}/, `${e}: ${t}
`);
        }
      });
    }
    const En = 100, Hr = 60, Ur = "    ";
    function qr({ source: e, line: t, column: n, baseLine: r, baseColumn: i }, o) {
      function s(w, k) {
        return a.slice(w, k).map(
          (C, b) => String(w + b + 1).padStart(d) + " |" + C
        ).join(`
`);
      }
      const c = `
`.repeat(Math.max(r - 1, 0)), l = " ".repeat(Math.max(i - 1, 0)), a = (c + l + e).split(/\r\n?|\n|\f/), u = Math.max(1, t - o) - 1, h = Math.min(t + o, a.length + 1), d = Math.max(4, String(h).length) + 1;
      let m = 0;
      n += (Ur.length - 1) * (a[t - 1].substr(0, n - 1).match(/\t/g) || []).length, n > En && (m = n - Hr + 3, n = Hr - 2);
      for (let w = u; w <= h; w++)
        w >= 0 && w < a.length && (a[w] = a[w].replace(/\t/g, Ur), a[w] = (m > 0 && a[w].length > m ? "…" : "") + a[w].substr(m, En - 2) + (a[w].length > m + En - 1 ? "…" : ""));
      return [
        s(u, t),
        new Array(n + d + 2).join("-") + "^",
        s(t, h)
      ].filter(Boolean).join(`
`).replace(/^(\s+\d+\s+\|\n)+/, "").replace(/\n(\s+\d+\s+\|)+$/, "");
    }
    function Gr(e, t, n, r, i, o = 1, s = 1) {
      return Object.assign(bn("SyntaxError", e), {
        source: t,
        offset: n,
        line: r,
        column: i,
        sourceFragment(l) {
          return qr({ source: t, line: r, column: i, baseLine: o, baseColumn: s }, isNaN(l) ? 0 : l);
        },
        get formattedMessage() {
          return `Parse error: ${e}
` + qr({ source: t, line: r, column: i, baseLine: o, baseColumn: s }, 2);
        }
      });
    }
    function Ya(e) {
      const t = this.createList();
      let n = !1;
      const r = {
        recognizer: e
      };
      for (; !this.eof; ) {
        switch (this.tokenType) {
          case X:
            this.next();
            continue;
          case W:
            n = !0, this.next();
            continue;
        }
        let i = e.getNode.call(this, r);
        if (i === void 0)
          break;
        n && (e.onWhiteSpace && e.onWhiteSpace.call(this, i, t, r), n = !1), t.push(i);
      }
      return n && e.onWhiteSpace && e.onWhiteSpace.call(this, null, t, r), t;
    }
    const Vr = () => {
    }, Qa = 33, Xa = 35, Ln = 59, Kr = 123, Yr = 0;
    function Za(e) {
      return function() {
        return this[e]();
      };
    }
    function $n(e) {
      const t = /* @__PURE__ */ Object.create(null);
      for (const n of Object.keys(e)) {
        const r = e[n], i = r.parse || r;
        i && (t[n] = i);
      }
      return t;
    }
    function Ja(e) {
      const t = {
        context: /* @__PURE__ */ Object.create(null),
        features: Object.assign(/* @__PURE__ */ Object.create(null), e.features),
        scope: Object.assign(/* @__PURE__ */ Object.create(null), e.scope),
        atrule: $n(e.atrule),
        pseudo: $n(e.pseudo),
        node: $n(e.node)
      };
      for (const [n, r] of Object.entries(e.parseContext))
        switch (typeof r) {
          case "function":
            t.context[n] = r;
            break;
          case "string":
            t.context[n] = Za(r);
            break;
        }
      return _(_({
        config: t
      }, t), t.node);
    }
    function el(e) {
      let t = "", n = "<unknown>", r = !1, i = Vr, o = !1;
      const s = new Ga(), c = Object.assign(new Ka(), Ja(e || {}), {
        parseAtrulePrelude: !0,
        parseRulePrelude: !0,
        parseValue: !0,
        parseCustomProperty: !1,
        readSequence: Ya,
        consumeUntilBalanceEnd: () => 0,
        consumeUntilLeftCurlyBracket(a) {
          return a === Kr ? 1 : 0;
        },
        consumeUntilLeftCurlyBracketOrSemicolon(a) {
          return a === Kr || a === Ln ? 1 : 0;
        },
        consumeUntilExclamationMarkOrSemicolon(a) {
          return a === Qa || a === Ln ? 1 : 0;
        },
        consumeUntilSemicolonIncluded(a) {
          return a === Ln ? 2 : 0;
        },
        createList() {
          return new q();
        },
        createSingleNodeList(a) {
          return new q().appendData(a);
        },
        getFirstListNode(a) {
          return a && a.first;
        },
        getLastListNode(a) {
          return a && a.last;
        },
        parseWithFallback(a, u) {
          const h = this.tokenIndex;
          try {
            return a.call(this);
          } catch (d) {
            if (o)
              throw d;
            this.skip(h - this.tokenIndex);
            const m = u.call(this);
            return o = !0, i(d, m), o = !1, m;
          }
        },
        lookupNonWSType(a) {
          let u;
          do
            if (u = this.lookupType(a++), u !== W && u !== X)
              return u;
          while (u !== Yr);
          return Yr;
        },
        charCodeAt(a) {
          return a >= 0 && a < t.length ? t.charCodeAt(a) : 0;
        },
        substring(a, u) {
          return t.substring(a, u);
        },
        substrToCursor(a) {
          return this.source.substring(a, this.tokenStart);
        },
        cmpChar(a, u) {
          return ht(t, a, u);
        },
        cmpStr(a, u, h) {
          return $t(t, a, u, h);
        },
        consume(a) {
          const u = this.tokenStart;
          return this.eat(a), this.substrToCursor(u);
        },
        consumeFunctionName() {
          const a = t.substring(this.tokenStart, this.tokenEnd - 1);
          return this.eat($), a;
        },
        consumeNumber(a) {
          const u = t.substring(this.tokenStart, mn(t, this.tokenStart));
          return this.eat(a), u;
        },
        eat(a) {
          if (this.tokenType !== a) {
            const u = mo[a].slice(0, -6).replace(/-/g, " ").replace(/^./, (m) => m.toUpperCase());
            let h = `${/[[\](){}]/.test(u) ? `"${u}"` : u} is expected`, d = this.tokenStart;
            switch (a) {
              case y:
                this.tokenType === $ || this.tokenType === te ? (d = this.tokenEnd - 1, h = "Identifier is expected but function found") : h = "Identifier is expected";
                break;
              case F:
                this.isDelim(Xa) && (this.next(), d++, h = "Name is expected");
                break;
              case B:
                this.tokenType === L && (d = this.tokenEnd, h = "Percent sign is expected");
                break;
            }
            this.error(h, d);
          }
          this.next();
        },
        eatIdent(a) {
          (this.tokenType !== y || this.lookupValue(0, a) === !1) && this.error(`Identifier "${a}" is expected`), this.next();
        },
        eatDelim(a) {
          this.isDelim(a) || this.error(`Delim "${String.fromCharCode(a)}" is expected`), this.next();
        },
        getLocation(a, u) {
          return r ? s.getLocationRange(
            a,
            u,
            n
          ) : null;
        },
        getLocationFromList(a) {
          if (r) {
            const u = this.getFirstListNode(a), h = this.getLastListNode(a);
            return s.getLocationRange(
              u !== null ? u.loc.start.offset - s.startOffset : this.tokenStart,
              h !== null ? h.loc.end.offset - s.startOffset : this.tokenStart,
              n
            );
          }
          return null;
        },
        error(a, u) {
          const h = typeof u != "undefined" && u < t.length ? s.getLocation(u) : this.eof ? s.getLocation(Ha(t, t.length - 1)) : s.getLocation(this.tokenStart);
          throw new Gr(
            a || "Unexpected input",
            t,
            h.offset,
            h.line,
            h.column,
            s.startLine,
            s.startColumn
          );
        }
      });
      return Object.assign(function(a, u) {
        t = a, u = u || {}, c.setSource(t, gn), s.setSource(
          t,
          u.offset,
          u.line,
          u.column
        ), n = u.filename || "<unknown>", r = !!u.positions, i = typeof u.onParseError == "function" ? u.onParseError : Vr, o = !1, c.parseAtrulePrelude = "parseAtrulePrelude" in u ? !!u.parseAtrulePrelude : !0, c.parseRulePrelude = "parseRulePrelude" in u ? !!u.parseRulePrelude : !0, c.parseValue = "parseValue" in u ? !!u.parseValue : !0, c.parseCustomProperty = "parseCustomProperty" in u ? !!u.parseCustomProperty : !1;
        const { context: h = "default", onComment: d } = u;
        if (!(h in c.context))
          throw new Error("Unknown context `" + h + "`");
        typeof d == "function" && c.forEachToken((w, k, C) => {
          if (w === X) {
            const b = c.getLocation(k, C), x = $t(t, C - 2, C, "*/") ? t.slice(k + 2, C - 2) : t.slice(k + 2, C);
            d(x, b);
          }
        });
        const m = c.context[h].call(c, u);
        return c.eof || c.error(), m;
      }, {
        SyntaxError: Gr,
        config: c.config
      });
    }
    var gr = {}, br = {}, Qr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".split("");
    br.encode = function(e) {
      if (0 <= e && e < Qr.length)
        return Qr[e];
      throw new TypeError("Must be between 0 and 63: " + e);
    };
    br.decode = function(e) {
      var t = 65, n = 90, r = 97, i = 122, o = 48, s = 57, c = 43, l = 47, a = 26, u = 52;
      return t <= e && e <= n ? e - t : r <= e && e <= i ? e - r + a : o <= e && e <= s ? e - o + u : e == c ? 62 : e == l ? 63 : -1;
    };
    var go = br, yr = 5, bo = 1 << yr, yo = bo - 1, ko = bo;
    function tl(e) {
      return e < 0 ? (-e << 1) + 1 : (e << 1) + 0;
    }
    function nl(e) {
      var t = (e & 1) === 1, n = e >> 1;
      return t ? -n : n;
    }
    gr.encode = function(t) {
      var n = "", r, i = tl(t);
      do
        r = i & yo, i >>>= yr, i > 0 && (r |= ko), n += go.encode(r);
      while (i > 0);
      return n;
    };
    gr.decode = function(t, n, r) {
      var i = t.length, o = 0, s = 0, c, l;
      do {
        if (n >= i)
          throw new Error("Expected more digits in base 64 VLQ value.");
        if (l = go.decode(t.charCodeAt(n++)), l === -1)
          throw new Error("Invalid base64 digit: " + t.charAt(n - 1));
        c = !!(l & ko), l &= yo, o = o + (l << s), s += yr;
      } while (c);
      r.value = nl(o), r.rest = n;
    };
    var yn = {};
    (function(e) {
      function t(f, p, S) {
        if (p in f)
          return f[p];
        if (arguments.length === 3)
          return S;
        throw new Error('"' + p + '" is a required argument.');
      }
      e.getArg = t;
      var n = /^(?:([\w+\-.]+):)?\/\/(?:(\w+:\w+)@)?([\w.-]*)(?::(\d+))?(.*)$/, r = /^data:.+\,.+$/;
      function i(f) {
        var p = f.match(n);
        return p ? {
          scheme: p[1],
          auth: p[2],
          host: p[3],
          port: p[4],
          path: p[5]
        } : null;
      }
      e.urlParse = i;
      function o(f) {
        var p = "";
        return f.scheme && (p += f.scheme + ":"), p += "//", f.auth && (p += f.auth + "@"), f.host && (p += f.host), f.port && (p += ":" + f.port), f.path && (p += f.path), p;
      }
      e.urlGenerate = o;
      var s = 32;
      function c(f) {
        var p = [];
        return function(S) {
          for (var g = 0; g < p.length; g++)
            if (p[g].input === S) {
              var K = p[0];
              return p[0] = p[g], p[g] = K, p[0].result;
            }
          var R = f(S);
          return p.unshift({
            input: S,
            result: R
          }), p.length > s && p.pop(), R;
        };
      }
      var l = c(function(p) {
        var S = p, g = i(p);
        if (g) {
          if (!g.path)
            return p;
          S = g.path;
        }
        for (var K = e.isAbsolute(S), R = [], be = 0, Y = 0; ; )
          if (be = Y, Y = S.indexOf("/", be), Y === -1) {
            R.push(S.slice(be));
            break;
          } else
            for (R.push(S.slice(be, Y)); Y < S.length && S[Y] === "/"; )
              Y++;
        for (var ze, Ue = 0, Y = R.length - 1; Y >= 0; Y--)
          ze = R[Y], ze === "." ? R.splice(Y, 1) : ze === ".." ? Ue++ : Ue > 0 && (ze === "" ? (R.splice(Y + 1, Ue), Ue = 0) : (R.splice(Y, 2), Ue--));
        return S = R.join("/"), S === "" && (S = K ? "/" : "."), g ? (g.path = S, o(g)) : S;
      });
      e.normalize = l;
      function a(f, p) {
        f === "" && (f = "."), p === "" && (p = ".");
        var S = i(p), g = i(f);
        if (g && (f = g.path || "/"), S && !S.scheme)
          return g && (S.scheme = g.scheme), o(S);
        if (S || p.match(r))
          return p;
        if (g && !g.host && !g.path)
          return g.host = p, o(g);
        var K = p.charAt(0) === "/" ? p : l(f.replace(/\/+$/, "") + "/" + p);
        return g ? (g.path = K, o(g)) : K;
      }
      e.join = a, e.isAbsolute = function(f) {
        return f.charAt(0) === "/" || n.test(f);
      };
      function u(f, p) {
        f === "" && (f = "."), f = f.replace(/\/$/, "");
        for (var S = 0; p.indexOf(f + "/") !== 0; ) {
          var g = f.lastIndexOf("/");
          if (g < 0 || (f = f.slice(0, g), f.match(/^([^\/]+:\/)?\/*$/)))
            return p;
          ++S;
        }
        return Array(S + 1).join("../") + p.substr(f.length + 1);
      }
      e.relative = u;
      var h = function() {
        var f = /* @__PURE__ */ Object.create(null);
        return !("__proto__" in f);
      }();
      function d(f) {
        return f;
      }
      function m(f) {
        return k(f) ? "$" + f : f;
      }
      e.toSetString = h ? d : m;
      function w(f) {
        return k(f) ? f.slice(1) : f;
      }
      e.fromSetString = h ? d : w;
      function k(f) {
        if (!f)
          return !1;
        var p = f.length;
        if (p < 9 || f.charCodeAt(p - 1) !== 95 || f.charCodeAt(p - 2) !== 95 || f.charCodeAt(p - 3) !== 111 || f.charCodeAt(p - 4) !== 116 || f.charCodeAt(p - 5) !== 111 || f.charCodeAt(p - 6) !== 114 || f.charCodeAt(p - 7) !== 112 || f.charCodeAt(p - 8) !== 95 || f.charCodeAt(p - 9) !== 95)
          return !1;
        for (var S = p - 10; S >= 0; S--)
          if (f.charCodeAt(S) !== 36)
            return !1;
        return !0;
      }
      function C(f, p, S) {
        var g = v(f.source, p.source);
        return g !== 0 || (g = f.originalLine - p.originalLine, g !== 0) || (g = f.originalColumn - p.originalColumn, g !== 0 || S) || (g = f.generatedColumn - p.generatedColumn, g !== 0) || (g = f.generatedLine - p.generatedLine, g !== 0) ? g : v(f.name, p.name);
      }
      e.compareByOriginalPositions = C;
      function b(f, p, S) {
        var g;
        return g = f.originalLine - p.originalLine, g !== 0 || (g = f.originalColumn - p.originalColumn, g !== 0 || S) || (g = f.generatedColumn - p.generatedColumn, g !== 0) || (g = f.generatedLine - p.generatedLine, g !== 0) ? g : v(f.name, p.name);
      }
      e.compareByOriginalPositionsNoSource = b;
      function x(f, p, S) {
        var g = f.generatedLine - p.generatedLine;
        return g !== 0 || (g = f.generatedColumn - p.generatedColumn, g !== 0 || S) || (g = v(f.source, p.source), g !== 0) || (g = f.originalLine - p.originalLine, g !== 0) || (g = f.originalColumn - p.originalColumn, g !== 0) ? g : v(f.name, p.name);
      }
      e.compareByGeneratedPositionsDeflated = x;
      function T(f, p, S) {
        var g = f.generatedColumn - p.generatedColumn;
        return g !== 0 || S || (g = v(f.source, p.source), g !== 0) || (g = f.originalLine - p.originalLine, g !== 0) || (g = f.originalColumn - p.originalColumn, g !== 0) ? g : v(f.name, p.name);
      }
      e.compareByGeneratedPositionsDeflatedNoLine = T;
      function v(f, p) {
        return f === p ? 0 : f === null ? 1 : p === null ? -1 : f > p ? 1 : -1;
      }
      function A(f, p) {
        var S = f.generatedLine - p.generatedLine;
        return S !== 0 || (S = f.generatedColumn - p.generatedColumn, S !== 0) || (S = v(f.source, p.source), S !== 0) || (S = f.originalLine - p.originalLine, S !== 0) || (S = f.originalColumn - p.originalColumn, S !== 0) ? S : v(f.name, p.name);
      }
      e.compareByGeneratedPositionsInflated = A;
      function P(f) {
        return JSON.parse(f.replace(/^\)]}'[^\n]*\n/, ""));
      }
      e.parseSourceMapInput = P;
      function O(f, p, S) {
        if (p = p || "", f && (f[f.length - 1] !== "/" && p[0] !== "/" && (f += "/"), p = f + p), S) {
          var g = i(S);
          if (!g)
            throw new Error("sourceMapURL could not be parsed");
          if (g.path) {
            var K = g.path.lastIndexOf("/");
            K >= 0 && (g.path = g.path.substring(0, K + 1));
          }
          p = a(o(g), p);
        }
        return l(p);
      }
      e.computeSourceURL = O;
    })(yn);
    var xo = {}, kr = yn, xr = Object.prototype.hasOwnProperty, Ye = typeof Map != "undefined";
    function Re() {
      this._array = [], this._set = Ye ? /* @__PURE__ */ new Map() : /* @__PURE__ */ Object.create(null);
    }
    Re.fromArray = function(t, n) {
      for (var r = new Re(), i = 0, o = t.length; i < o; i++)
        r.add(t[i], n);
      return r;
    };
    Re.prototype.size = function() {
      return Ye ? this._set.size : Object.getOwnPropertyNames(this._set).length;
    };
    Re.prototype.add = function(t, n) {
      var r = Ye ? t : kr.toSetString(t), i = Ye ? this.has(t) : xr.call(this._set, r), o = this._array.length;
      (!i || n) && this._array.push(t), i || (Ye ? this._set.set(t, o) : this._set[r] = o);
    };
    Re.prototype.has = function(t) {
      if (Ye)
        return this._set.has(t);
      var n = kr.toSetString(t);
      return xr.call(this._set, n);
    };
    Re.prototype.indexOf = function(t) {
      if (Ye) {
        var n = this._set.get(t);
        if (n >= 0)
          return n;
      } else {
        var r = kr.toSetString(t);
        if (xr.call(this._set, r))
          return this._set[r];
      }
      throw new Error('"' + t + '" is not in the set.');
    };
    Re.prototype.at = function(t) {
      if (t >= 0 && t < this._array.length)
        return this._array[t];
      throw new Error("No element indexed by " + t);
    };
    Re.prototype.toArray = function() {
      return this._array.slice();
    };
    xo.ArraySet = Re;
    var wo = {}, vo = yn;
    function rl(e, t) {
      var n = e.generatedLine, r = t.generatedLine, i = e.generatedColumn, o = t.generatedColumn;
      return r > n || r == n && o >= i || vo.compareByGeneratedPositionsInflated(e, t) <= 0;
    }
    function kn() {
      this._array = [], this._sorted = !0, this._last = { generatedLine: -1, generatedColumn: 0 };
    }
    kn.prototype.unsortedForEach = function(t, n) {
      this._array.forEach(t, n);
    };
    kn.prototype.add = function(t) {
      rl(this._last, t) ? (this._last = t, this._array.push(t)) : (this._sorted = !1, this._array.push(t));
    };
    kn.prototype.toArray = function() {
      return this._sorted || (this._array.sort(vo.compareByGeneratedPositionsInflated), this._sorted = !0), this._array;
    };
    wo.MappingList = kn;
    var yt = gr, U = yn, rn = xo.ArraySet, il = wo.MappingList;
    function ge(e) {
      e || (e = {}), this._file = U.getArg(e, "file", null), this._sourceRoot = U.getArg(e, "sourceRoot", null), this._skipValidation = U.getArg(e, "skipValidation", !1), this._ignoreInvalidMapping = U.getArg(e, "ignoreInvalidMapping", !1), this._sources = new rn(), this._names = new rn(), this._mappings = new il(), this._sourcesContents = null;
    }
    ge.prototype._version = 3;
    ge.fromSourceMap = function(t, n) {
      var r = t.sourceRoot, i = new ge(Object.assign(n || {}, {
        file: t.file,
        sourceRoot: r
      }));
      return t.eachMapping(function(o) {
        var s = {
          generated: {
            line: o.generatedLine,
            column: o.generatedColumn
          }
        };
        o.source != null && (s.source = o.source, r != null && (s.source = U.relative(r, s.source)), s.original = {
          line: o.originalLine,
          column: o.originalColumn
        }, o.name != null && (s.name = o.name)), i.addMapping(s);
      }), t.sources.forEach(function(o) {
        var s = o;
        r !== null && (s = U.relative(r, o)), i._sources.has(s) || i._sources.add(s);
        var c = t.sourceContentFor(o);
        c != null && i.setSourceContent(o, c);
      }), i;
    };
    ge.prototype.addMapping = function(t) {
      var n = U.getArg(t, "generated"), r = U.getArg(t, "original", null), i = U.getArg(t, "source", null), o = U.getArg(t, "name", null);
      !this._skipValidation && this._validateMapping(n, r, i, o) === !1 || (i != null && (i = String(i), this._sources.has(i) || this._sources.add(i)), o != null && (o = String(o), this._names.has(o) || this._names.add(o)), this._mappings.add({
        generatedLine: n.line,
        generatedColumn: n.column,
        originalLine: r != null && r.line,
        originalColumn: r != null && r.column,
        source: i,
        name: o
      }));
    };
    ge.prototype.setSourceContent = function(t, n) {
      var r = t;
      this._sourceRoot != null && (r = U.relative(this._sourceRoot, r)), n != null ? (this._sourcesContents || (this._sourcesContents = /* @__PURE__ */ Object.create(null)), this._sourcesContents[U.toSetString(r)] = n) : this._sourcesContents && (delete this._sourcesContents[U.toSetString(r)], Object.keys(this._sourcesContents).length === 0 && (this._sourcesContents = null));
    };
    ge.prototype.applySourceMap = function(t, n, r) {
      var i = n;
      if (n == null) {
        if (t.file == null)
          throw new Error(
            `SourceMapGenerator.prototype.applySourceMap requires either an explicit source file, or the source map's "file" property. Both were omitted.`
          );
        i = t.file;
      }
      var o = this._sourceRoot;
      o != null && (i = U.relative(o, i));
      var s = new rn(), c = new rn();
      this._mappings.unsortedForEach(function(l) {
        if (l.source === i && l.originalLine != null) {
          var a = t.originalPositionFor({
            line: l.originalLine,
            column: l.originalColumn
          });
          a.source != null && (l.source = a.source, r != null && (l.source = U.join(r, l.source)), o != null && (l.source = U.relative(o, l.source)), l.originalLine = a.line, l.originalColumn = a.column, a.name != null && (l.name = a.name));
        }
        var u = l.source;
        u != null && !s.has(u) && s.add(u);
        var h = l.name;
        h != null && !c.has(h) && c.add(h);
      }, this), this._sources = s, this._names = c, t.sources.forEach(function(l) {
        var a = t.sourceContentFor(l);
        a != null && (r != null && (l = U.join(r, l)), o != null && (l = U.relative(o, l)), this.setSourceContent(l, a));
      }, this);
    };
    ge.prototype._validateMapping = function(t, n, r, i) {
      if (n && typeof n.line != "number" && typeof n.column != "number") {
        var o = "original.line and original.column are not numbers -- you probably meant to omit the original mapping entirely and only map the generated position. If so, pass null for the original mapping instead of an object with empty or null values.";
        if (this._ignoreInvalidMapping)
          return typeof console != "undefined" && console.warn && console.warn(o), !1;
        throw new Error(o);
      }
      if (!(t && "line" in t && "column" in t && t.line > 0 && t.column >= 0 && !n && !r && !i)) {
        if (t && "line" in t && "column" in t && n && "line" in n && "column" in n && t.line > 0 && t.column >= 0 && n.line > 0 && n.column >= 0 && r)
          return;
        var o = "Invalid mapping: " + JSON.stringify({
          generated: t,
          source: r,
          original: n,
          name: i
        });
        if (this._ignoreInvalidMapping)
          return typeof console != "undefined" && console.warn && console.warn(o), !1;
        throw new Error(o);
      }
    };
    ge.prototype._serializeMappings = function() {
      for (var t = 0, n = 1, r = 0, i = 0, o = 0, s = 0, c = "", l, a, u, h, d = this._mappings.toArray(), m = 0, w = d.length; m < w; m++) {
        if (a = d[m], l = "", a.generatedLine !== n)
          for (t = 0; a.generatedLine !== n; )
            l += ";", n++;
        else if (m > 0) {
          if (!U.compareByGeneratedPositionsInflated(a, d[m - 1]))
            continue;
          l += ",";
        }
        l += yt.encode(a.generatedColumn - t), t = a.generatedColumn, a.source != null && (h = this._sources.indexOf(a.source), l += yt.encode(h - s), s = h, l += yt.encode(a.originalLine - 1 - i), i = a.originalLine - 1, l += yt.encode(a.originalColumn - r), r = a.originalColumn, a.name != null && (u = this._names.indexOf(a.name), l += yt.encode(u - o), o = u)), c += l;
      }
      return c;
    };
    ge.prototype._generateSourcesContent = function(t, n) {
      return t.map(function(r) {
        if (!this._sourcesContents)
          return null;
        n != null && (r = U.relative(n, r));
        var i = U.toSetString(r);
        return Object.prototype.hasOwnProperty.call(this._sourcesContents, i) ? this._sourcesContents[i] : null;
      }, this);
    };
    ge.prototype.toJSON = function() {
      var t = {
        version: this._version,
        sources: this._sources.toArray(),
        names: this._names.toArray(),
        mappings: this._serializeMappings()
      };
      return this._file != null && (t.file = this._file), this._sourceRoot != null && (t.sourceRoot = this._sourceRoot), this._sourcesContents && (t.sourcesContent = this._generateSourcesContent(t.sources, t.sourceRoot)), t;
    };
    ge.prototype.toString = function() {
      return JSON.stringify(this.toJSON());
    };
    var ol = ge;
    const Xr = /* @__PURE__ */ new Set(["Atrule", "Selector", "Declaration"]);
    function sl(e) {
      const t = new ol(), n = {
        line: 1,
        column: 0
      }, r = {
        line: 0,
        // should be zero to add first mapping
        column: 0
      }, i = {
        line: 1,
        column: 0
      }, o = {
        generated: i
      };
      let s = 1, c = 0, l = !1;
      const a = e.node;
      e.node = function(d) {
        if (d.loc && d.loc.start && Xr.has(d.type)) {
          const m = d.loc.start.line, w = d.loc.start.column - 1;
          (r.line !== m || r.column !== w) && (r.line = m, r.column = w, n.line = s, n.column = c, l && (l = !1, (n.line !== i.line || n.column !== i.column) && t.addMapping(o)), l = !0, t.addMapping({
            source: d.loc.source,
            original: r,
            generated: n
          }));
        }
        a.call(this, d), l && Xr.has(d.type) && (i.line = s, i.column = c);
      };
      const u = e.emit;
      e.emit = function(d, m, w) {
        for (let k = 0; k < d.length; k++)
          d.charCodeAt(k) === 10 ? (s++, c = 0) : c++;
        u(d, m, w);
      };
      const h = e.result;
      return e.result = function() {
        return l && t.addMapping(o), {
          css: h(),
          map: t
        };
      }, e;
    }
    const al = 43, ll = 45, _n = (e, t) => {
      if (e === I && (e = t), typeof e == "string") {
        const n = e.charCodeAt(0);
        return n > 127 ? 32768 : n << 8;
      }
      return e;
    }, So = [
      [y, y],
      [y, $],
      [y, te],
      [y, se],
      [y, "-"],
      [y, L],
      [y, B],
      [y, z],
      [y, ae],
      [y, M],
      [G, y],
      [G, $],
      [G, te],
      [G, se],
      [G, "-"],
      [G, L],
      [G, B],
      [G, z],
      [G, ae],
      [F, y],
      [F, $],
      [F, te],
      [F, se],
      [F, "-"],
      [F, L],
      [F, B],
      [F, z],
      [F, ae],
      [z, y],
      [z, $],
      [z, te],
      [z, se],
      [z, "-"],
      [z, L],
      [z, B],
      [z, z],
      [z, ae],
      ["#", y],
      ["#", $],
      ["#", te],
      ["#", se],
      ["#", "-"],
      ["#", L],
      ["#", B],
      ["#", z],
      ["#", ae],
      // https://github.com/w3c/csswg-drafts/pull/6874
      ["-", y],
      ["-", $],
      ["-", te],
      ["-", se],
      ["-", "-"],
      ["-", L],
      ["-", B],
      ["-", z],
      ["-", ae],
      // https://github.com/w3c/csswg-drafts/pull/6874
      [L, y],
      [L, $],
      [L, te],
      [L, se],
      [L, L],
      [L, B],
      [L, z],
      [L, "%"],
      [L, ae],
      // https://github.com/w3c/csswg-drafts/pull/6874
      ["@", y],
      ["@", $],
      ["@", te],
      ["@", se],
      ["@", "-"],
      ["@", ae],
      // https://github.com/w3c/csswg-drafts/pull/6874
      [".", L],
      [".", B],
      [".", z],
      ["+", L],
      ["+", B],
      ["+", z],
      ["/", "*"]
    ], cl = So.concat([
      [y, F],
      [z, F],
      [F, F],
      [G, M],
      [G, Te],
      [G, ne],
      [B, B],
      [B, z],
      [B, $],
      [B, "-"],
      [E, y],
      [E, $],
      [E, B],
      [E, z],
      [E, F],
      [E, "-"]
    ]);
    function Co(e) {
      const t = new Set(
        e.map(([n, r]) => _n(n) << 16 | _n(r))
      );
      return function(n, r, i) {
        const o = _n(r, i), s = i.charCodeAt(0);
        return (s === ll && r !== y && r !== $ && r !== ae || s === al ? t.has(n << 16 | s << 8) : t.has(n << 16 | o)) && this.emit(" ", W, !0), o;
      };
    }
    const ul = Co(So), To = Co(cl), Zr = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      safe: To,
      spec: ul
    }, Symbol.toStringTag, { value: "Module" })), hl = 92;
    function fl(e, t) {
      if (typeof t == "function") {
        let n = null;
        e.children.forEach((r) => {
          n !== null && t.call(this, n), this.node(r), n = r;
        });
        return;
      }
      e.children.forEach(this.node, this);
    }
    function pl(e) {
      gn(e, (t, n, r) => {
        this.token(t, e.slice(n, r));
      });
    }
    function dl(e) {
      const t = /* @__PURE__ */ new Map();
      for (let [n, r] of Object.entries(e.node))
        typeof (r.generate || r) == "function" && t.set(n, r.generate || r);
      return function(n, r) {
        let i = "", o = 0, s = {
          node(l) {
            if (t.has(l.type))
              t.get(l.type).call(c, l);
            else
              throw new Error("Unknown node type: " + l.type);
          },
          tokenBefore: To,
          token(l, a) {
            o = this.tokenBefore(o, l, a), this.emit(a, l, !1), l === I && a.charCodeAt(0) === hl && this.emit(`
`, W, !0);
          },
          emit(l) {
            i += l;
          },
          result() {
            return i;
          }
        };
        r && (typeof r.decorator == "function" && (s = r.decorator(s)), r.sourceMap && (s = sl(s)), r.mode in Zr && (s.tokenBefore = Zr[r.mode]));
        const c = {
          node: (l) => s.node(l),
          children: fl,
          token: (l, a) => s.token(l, a),
          tokenize: pl
        };
        return s.node(n), s.result();
      };
    }
    function ml(e) {
      return {
        fromPlainObject(t) {
          return e(t, {
            enter(n) {
              n.children && !(n.children instanceof q) && (n.children = new q().fromArray(n.children));
            }
          }), t;
        },
        toPlainObject(t) {
          return e(t, {
            leave(n) {
              n.children && n.children instanceof q && (n.children = n.children.toArray());
            }
          }), t;
        }
      };
    }
    const { hasOwnProperty: wr } = Object.prototype, xt = function() {
    };
    function Jr(e) {
      return typeof e == "function" ? e : xt;
    }
    function ei(e, t) {
      return function(n, r, i) {
        n.type === t && e.call(this, n, r, i);
      };
    }
    function gl(e, t) {
      const n = t.structure, r = [];
      for (const i in n) {
        if (wr.call(n, i) === !1)
          continue;
        let o = n[i];
        const s = {
          name: i,
          type: !1,
          nullable: !1
        };
        Array.isArray(o) || (o = [o]);
        for (const c of o)
          c === null ? s.nullable = !0 : typeof c == "string" ? s.type = "node" : Array.isArray(c) && (s.type = "list");
        s.type && r.push(s);
      }
      return r.length ? {
        context: t.walkContext,
        fields: r
      } : null;
    }
    function bl(e) {
      const t = {};
      for (const n in e.node)
        if (wr.call(e.node, n)) {
          const r = e.node[n];
          if (!r.structure)
            throw new Error("Missed `structure` field in `" + n + "` node type definition");
          t[n] = gl(n, r);
        }
      return t;
    }
    function ti(e, t) {
      const n = e.fields.slice(), r = e.context, i = typeof r == "string";
      return t && n.reverse(), function(o, s, c, l) {
        let a;
        i && (a = s[r], s[r] = o);
        for (const u of n) {
          const h = o[u.name];
          if (!u.nullable || h) {
            if (u.type === "list") {
              if (t ? h.reduceRight(l, !1) : h.reduce(l, !1))
                return !0;
            } else if (c(h))
              return !0;
          }
        }
        i && (s[r] = a);
      };
    }
    function ni({
      StyleSheet: e,
      Atrule: t,
      Rule: n,
      Block: r,
      DeclarationList: i
    }) {
      return {
        Atrule: {
          StyleSheet: e,
          Atrule: t,
          Rule: n,
          Block: r
        },
        Rule: {
          StyleSheet: e,
          Atrule: t,
          Rule: n,
          Block: r
        },
        Declaration: {
          StyleSheet: e,
          Atrule: t,
          Rule: n,
          Block: r,
          DeclarationList: i
        }
      };
    }
    function yl(e) {
      const t = bl(e), n = {}, r = {}, i = Symbol("break-walk"), o = Symbol("skip-node");
      for (const a in t)
        wr.call(t, a) && t[a] !== null && (n[a] = ti(t[a], !1), r[a] = ti(t[a], !0));
      const s = ni(n), c = ni(r), l = function(a, u) {
        function h(b, x, T) {
          const v = d.call(C, b, x, T);
          return v === i ? !0 : v === o ? !1 : !!(w.hasOwnProperty(b.type) && w[b.type](b, C, h, k) || m.call(C, b, x, T) === i);
        }
        let d = xt, m = xt, w = n, k = (b, x, T, v) => b || h(x, T, v);
        const C = {
          break: i,
          skip: o,
          root: a,
          stylesheet: null,
          atrule: null,
          atrulePrelude: null,
          rule: null,
          selector: null,
          block: null,
          declaration: null,
          function: null
        };
        if (typeof u == "function")
          d = u;
        else if (u && (d = Jr(u.enter), m = Jr(u.leave), u.reverse && (w = r), u.visit)) {
          if (s.hasOwnProperty(u.visit))
            w = u.reverse ? c[u.visit] : s[u.visit];
          else if (!t.hasOwnProperty(u.visit))
            throw new Error("Bad value `" + u.visit + "` for `visit` option (should be: " + Object.keys(t).sort().join(", ") + ")");
          d = ei(d, u.visit), m = ei(m, u.visit);
        }
        if (d === xt && m === xt)
          throw new Error("Neither `enter` nor `leave` walker handler is set or both aren't a function");
        h(a);
      };
      return l.break = i, l.skip = o, l.find = function(a, u) {
        let h = null;
        return l(a, function(d, m, w) {
          if (u.call(this, d, m, w))
            return h = d, i;
        }), h;
      }, l.findLast = function(a, u) {
        let h = null;
        return l(a, {
          reverse: !0,
          enter(d, m, w) {
            if (u.call(this, d, m, w))
              return h = d, i;
          }
        }), h;
      }, l.findAll = function(a, u) {
        const h = [];
        return l(a, function(d, m, w) {
          u.call(this, d, m, w) && h.push(d);
        }), h;
      }, l;
    }
    function kl(e) {
      return e;
    }
    function xl(e) {
      const { min: t, max: n, comma: r } = e;
      return t === 0 && n === 0 ? r ? "#?" : "*" : t === 0 && n === 1 ? "?" : t === 1 && n === 0 ? r ? "#" : "+" : t === 1 && n === 1 ? "" : (r ? "#" : "") + (t === n ? "{" + t + "}" : "{" + t + "," + (n !== 0 ? n : "") + "}");
    }
    function wl(e) {
      switch (e.type) {
        case "Range":
          return " [" + (e.min === null ? "-∞" : e.min) + "," + (e.max === null ? "∞" : e.max) + "]";
        default:
          throw new Error("Unknown node type `" + e.type + "`");
      }
    }
    function vl(e, t, n, r) {
      const i = e.combinator === " " || r ? e.combinator : " " + e.combinator + " ", o = e.terms.map((s) => vr(s, t, n, r)).join(i);
      return e.explicit || n ? (r || o[0] === "," ? "[" : "[ ") + o + (r ? "]" : " ]") : o;
    }
    function vr(e, t, n, r) {
      let i;
      switch (e.type) {
        case "Group":
          i = vl(e, t, n, r) + (e.disallowEmpty ? "!" : "");
          break;
        case "Multiplier":
          return vr(e.term, t, n, r) + t(xl(e), e);
        case "Type":
          i = "<" + e.name + (e.opts ? t(wl(e.opts), e.opts) : "") + ">";
          break;
        case "Property":
          i = "<'" + e.name + "'>";
          break;
        case "Keyword":
          i = e.name;
          break;
        case "AtKeyword":
          i = "@" + e.name;
          break;
        case "Function":
          i = e.name + "(";
          break;
        case "String":
        case "Token":
          i = e.value;
          break;
        case "Comma":
          i = ",";
          break;
        default:
          throw new Error("Unknown node type `" + e.type + "`");
      }
      return t(i, e);
    }
    function Sr(e, t) {
      let n = kl, r = !1, i = !1;
      return typeof t == "function" ? n = t : t && (r = !!t.forceBraces, i = !!t.compact, typeof t.decorate == "function" && (n = t.decorate)), vr(e, n, r, i);
    }
    const ri = { offset: 0, line: 1, column: 1 };
    function Sl(e, t) {
      const n = e.tokens, r = e.longestMatch, i = r < n.length && n[r].node || null, o = i !== t ? i : null;
      let s = 0, c = 0, l = 0, a = "", u, h;
      for (let d = 0; d < n.length; d++) {
        const m = n[d].value;
        d === r && (c = m.length, s = a.length), o !== null && n[d].node === o && (d <= r ? l++ : l = 0), a += m;
      }
      return r === n.length || l > 1 ? (u = jt(o || t, "end") || wt(ri, a), h = wt(u)) : (u = jt(o, "start") || wt(jt(t, "start") || ri, a.slice(0, s)), h = jt(o, "end") || wt(u, a.substr(s, c))), {
        css: a,
        mismatchOffset: s,
        mismatchLength: c,
        start: u,
        end: h
      };
    }
    function jt(e, t) {
      const n = e && e.loc && e.loc[t];
      return n ? "line" in n ? wt(n) : n : null;
    }
    function wt({ offset: e, line: t, column: n }, r) {
      const i = {
        offset: e,
        line: t,
        column: n
      };
      if (r) {
        const o = r.split(/\n|\r\n?|\f/);
        i.offset += r.length, i.line += o.length - 1, i.column = o.length === 1 ? i.column + r.length : o.pop().length + 1;
      }
      return i;
    }
    const kt = function(e, t) {
      const n = bn(
        "SyntaxReferenceError",
        e + (t ? " `" + t + "`" : "")
      );
      return n.reference = t, n;
    }, Cl = function(e, t, n, r) {
      const i = bn("SyntaxMatchError", e), {
        css: o,
        mismatchOffset: s,
        mismatchLength: c,
        start: l,
        end: a
      } = Sl(r, n);
      return i.rawMessage = e, i.syntax = t ? Sr(t) : "<generic>", i.css = o, i.mismatchOffset = s, i.mismatchLength = c, i.message = e + `
  syntax: ` + i.syntax + `
   value: ` + (o || "<empty string>") + `
  --------` + new Array(i.mismatchOffset + 1).join("-") + "^", Object.assign(i, l), i.loc = {
        source: n && n.loc && n.loc.source || "<unknown>",
        start: l,
        end: a
      }, i;
    }, Ft = /* @__PURE__ */ new Map(), rt = /* @__PURE__ */ new Map(), on = 45, Pn = Tl, ii = Al;
    function Cr(e, t) {
      return t = t || 0, e.length - t >= 2 && e.charCodeAt(t) === on && e.charCodeAt(t + 1) === on;
    }
    function Ao(e, t) {
      if (t = t || 0, e.length - t >= 3 && e.charCodeAt(t) === on && e.charCodeAt(t + 1) !== on) {
        const n = e.indexOf("-", t + 2);
        if (n !== -1)
          return e.substring(t, n + 1);
      }
      return "";
    }
    function Tl(e) {
      if (Ft.has(e))
        return Ft.get(e);
      const t = e.toLowerCase();
      let n = Ft.get(t);
      if (n === void 0) {
        const r = Cr(t, 0), i = r ? "" : Ao(t, 0);
        n = Object.freeze({
          basename: t.substr(i.length),
          name: t,
          prefix: i,
          vendor: i,
          custom: r
        });
      }
      return Ft.set(e, n), n;
    }
    function Al(e) {
      if (rt.has(e))
        return rt.get(e);
      let t = e, n = e[0];
      n === "/" ? n = e[1] === "/" ? "//" : "/" : n !== "_" && n !== "*" && n !== "$" && n !== "#" && n !== "+" && n !== "&" && (n = "");
      const r = Cr(t, n.length);
      if (!r && (t = t.toLowerCase(), rt.has(t))) {
        const c = rt.get(t);
        return rt.set(e, c), c;
      }
      const i = r ? "" : Ao(t, n.length), o = t.substr(0, n.length + i.length), s = Object.freeze({
        basename: t.substr(o.length),
        name: t.substr(n.length),
        hack: n,
        vendor: i,
        prefix: o,
        custom: r
      });
      return rt.set(e, s), s;
    }
    const Oo = [
      "initial",
      "inherit",
      "unset",
      "revert",
      "revert-layer"
    ], _t = 43, Oe = 45, zn = 110, it = !0, Ol = !1;
    function Qn(e, t) {
      return e !== null && e.type === I && e.value.charCodeAt(0) === t;
    }
    function Tt(e, t, n) {
      for (; e !== null && (e.type === W || e.type === X); )
        e = n(++t);
      return t;
    }
    function Ne(e, t, n, r) {
      if (!e)
        return 0;
      const i = e.value.charCodeAt(t);
      if (i === _t || i === Oe) {
        if (n)
          return 0;
        t++;
      }
      for (; t < e.value.length; t++)
        if (!Q(e.value.charCodeAt(t)))
          return 0;
      return r + 1;
    }
    function In(e, t, n) {
      let r = !1, i = Tt(e, t, n);
      if (e = n(i), e === null)
        return t;
      if (e.type !== L)
        if (Qn(e, _t) || Qn(e, Oe)) {
          if (r = !0, i = Tt(n(++i), i, n), e = n(i), e === null || e.type !== L)
            return 0;
        } else
          return t;
      if (!r) {
        const o = e.value.charCodeAt(0);
        if (o !== _t && o !== Oe)
          return 0;
      }
      return Ne(e, r ? 0 : 1, r, i);
    }
    function El(e, t) {
      let n = 0;
      if (!e)
        return 0;
      if (e.type === L)
        return Ne(e, 0, Ol, n);
      if (e.type === y && e.value.charCodeAt(0) === Oe) {
        if (!ht(e.value, 1, zn))
          return 0;
        switch (e.value.length) {
          case 2:
            return In(t(++n), n, t);
          case 3:
            return e.value.charCodeAt(2) !== Oe ? 0 : (n = Tt(t(++n), n, t), e = t(n), Ne(e, 0, it, n));
          default:
            return e.value.charCodeAt(2) !== Oe ? 0 : Ne(e, 3, it, n);
        }
      } else if (e.type === y || Qn(e, _t) && t(n + 1).type === y) {
        if (e.type !== y && (e = t(++n)), e === null || !ht(e.value, 0, zn))
          return 0;
        switch (e.value.length) {
          case 1:
            return In(t(++n), n, t);
          case 2:
            return e.value.charCodeAt(1) !== Oe ? 0 : (n = Tt(t(++n), n, t), e = t(n), Ne(e, 0, it, n));
          default:
            return e.value.charCodeAt(1) !== Oe ? 0 : Ne(e, 2, it, n);
        }
      } else if (e.type === z) {
        let r = e.value.charCodeAt(0), i = r === _t || r === Oe ? 1 : 0, o = i;
        for (; o < e.value.length && Q(e.value.charCodeAt(o)); o++)
          ;
        return o === i || !ht(e.value, o, zn) ? 0 : o + 1 === e.value.length ? In(t(++n), n, t) : e.value.charCodeAt(o + 1) !== Oe ? 0 : o + 2 === e.value.length ? (n = Tt(t(++n), n, t), e = t(n), Ne(e, 0, it, n)) : Ne(e, o + 2, it, n);
      }
      return 0;
    }
    const Ll = 43, Eo = 45, Lo = 63, $l = 117;
    function Xn(e, t) {
      return e !== null && e.type === I && e.value.charCodeAt(0) === t;
    }
    function _l(e, t) {
      return e.value.charCodeAt(0) === t;
    }
    function vt(e, t, n) {
      let r = 0;
      for (let i = t; i < e.value.length; i++) {
        const o = e.value.charCodeAt(i);
        if (o === Eo && n && r !== 0)
          return vt(e, t + r + 1, !1), 6;
        if (!He(o) || ++r > 6)
          return 0;
      }
      return r;
    }
    function Bt(e, t, n) {
      if (!e)
        return 0;
      for (; Xn(n(t), Lo); ) {
        if (++e > 6)
          return 0;
        t++;
      }
      return t;
    }
    function Pl(e, t) {
      let n = 0;
      if (e === null || e.type !== y || !ht(e.value, 0, $l) || (e = t(++n), e === null))
        return 0;
      if (Xn(e, Ll))
        return e = t(++n), e === null ? 0 : e.type === y ? Bt(vt(e, 0, !0), ++n, t) : Xn(e, Lo) ? Bt(1, ++n, t) : 0;
      if (e.type === L) {
        const r = vt(e, 1, !0);
        return r === 0 ? 0 : (e = t(++n), e === null ? n : e.type === z || e.type === L ? !_l(e, Eo) || !vt(e, 1, !1) ? 0 : n + 1 : Bt(r, n, t));
      }
      return e.type === z ? Bt(vt(e, 1, !0), ++n, t) : 0;
    }
    const zl = ["calc(", "-moz-calc(", "-webkit-calc("], Tr = /* @__PURE__ */ new Map([
      [$, E],
      [M, E],
      [pe, Se],
      [V, ue]
    ]);
    function xe(e, t) {
      return t < e.length ? e.charCodeAt(t) : 0;
    }
    function $o(e, t) {
      return $t(e, 0, e.length, t);
    }
    function _o(e, t) {
      for (let n = 0; n < t.length; n++)
        if ($o(e, t[n]))
          return !0;
      return !1;
    }
    function Po(e, t) {
      return t !== e.length - 2 ? !1 : xe(e, t) === 92 && // U+005C REVERSE SOLIDUS (\)
      Q(xe(e, t + 1));
    }
    function xn(e, t, n) {
      if (e && e.type === "Range") {
        const r = Number(
          n !== void 0 && n !== t.length ? t.substr(0, n) : t
        );
        if (isNaN(r) || e.min !== null && r < e.min && typeof e.min != "string" || e.max !== null && r > e.max && typeof e.max != "string")
          return !0;
      }
      return !1;
    }
    function Il(e, t) {
      let n = 0, r = [], i = 0;
      e:
        do {
          switch (e.type) {
            case ue:
            case E:
            case Se:
              if (e.type !== n)
                break e;
              if (n = r.pop(), r.length === 0) {
                i++;
                break e;
              }
              break;
            case $:
            case M:
            case pe:
            case V:
              r.push(n), n = Tr.get(e.type);
              break;
          }
          i++;
        } while (e = t(i));
      return i;
    }
    function me(e) {
      return function(t, n, r) {
        return t === null ? 0 : t.type === $ && _o(t.value, zl) ? Il(t, n) : e(t, n, r);
      };
    }
    function j(e) {
      return function(t) {
        return t === null || t.type !== e ? 0 : 1;
      };
    }
    function Rl(e) {
      if (e === null || e.type !== y)
        return 0;
      const t = e.value.toLowerCase();
      return _o(t, Oo) || $o(t, "default") ? 0 : 1;
    }
    function zo(e) {
      return e === null || e.type !== y || xe(e.value, 0) !== 45 || xe(e.value, 1) !== 45 ? 0 : 1;
    }
    function Ml(e) {
      return !zo(e) || e.value === "--" ? 0 : 1;
    }
    function Nl(e) {
      if (e === null || e.type !== F)
        return 0;
      const t = e.value.length;
      if (t !== 4 && t !== 5 && t !== 7 && t !== 9)
        return 0;
      for (let n = 1; n < t; n++)
        if (!He(xe(e.value, n)))
          return 0;
      return 1;
    }
    function Dl(e) {
      return e === null || e.type !== F || !qt(xe(e.value, 1), xe(e.value, 2), xe(e.value, 3)) ? 0 : 1;
    }
    function jl(e, t) {
      if (!e)
        return 0;
      let n = 0, r = [], i = 0;
      e:
        do {
          switch (e.type) {
            case dn:
            case se:
              break e;
            case ue:
            case E:
            case Se:
              if (e.type !== n)
                break e;
              n = r.pop();
              break;
            case re:
              if (n === 0)
                break e;
              break;
            case I:
              if (n === 0 && e.value === "!")
                break e;
              break;
            case $:
            case M:
            case pe:
            case V:
              r.push(n), n = Tr.get(e.type);
              break;
          }
          i++;
        } while (e = t(i));
      return i;
    }
    function Fl(e, t) {
      if (!e)
        return 0;
      let n = 0, r = [], i = 0;
      e:
        do {
          switch (e.type) {
            case dn:
            case se:
              break e;
            case ue:
            case E:
            case Se:
              if (e.type !== n)
                break e;
              n = r.pop();
              break;
            case $:
            case M:
            case pe:
            case V:
              r.push(n), n = Tr.get(e.type);
              break;
          }
          i++;
        } while (e = t(i));
      return i;
    }
    function Ie(e) {
      return e && (e = new Set(e)), function(t, n, r) {
        if (t === null || t.type !== z)
          return 0;
        const i = mn(t.value, 0);
        if (e !== null) {
          const o = t.value.indexOf("\\", i), s = o === -1 || !Po(t.value, o) ? t.value.substr(i) : t.value.substring(i, o);
          if (e.has(s.toLowerCase()) === !1)
            return 0;
        }
        return xn(r, t.value, i) ? 0 : 1;
      };
    }
    function Bl(e, t, n) {
      return e === null || e.type !== B || xn(n, e.value, e.value.length - 1) ? 0 : 1;
    }
    function Io(e) {
      return typeof e != "function" && (e = function() {
        return 0;
      }), function(t, n, r) {
        return t !== null && t.type === L && Number(t.value) === 0 ? 1 : e(t, n, r);
      };
    }
    function Wl(e, t, n) {
      if (e === null)
        return 0;
      const r = mn(e.value, 0);
      return !(r === e.value.length) && !Po(e.value, r) || xn(n, e.value, r) ? 0 : 1;
    }
    function Hl(e, t, n) {
      if (e === null || e.type !== L)
        return 0;
      let r = xe(e.value, 0) === 43 || // U+002B PLUS SIGN (+)
      xe(e.value, 0) === 45 ? 1 : 0;
      for (; r < e.value.length; r++)
        if (!Q(xe(e.value, r)))
          return 0;
      return xn(n, e.value, r) ? 0 : 1;
    }
    const Ul = {
      "ident-token": j(y),
      "function-token": j($),
      "at-keyword-token": j(G),
      "hash-token": j(F),
      "string-token": j(Te),
      "bad-string-token": j(dn),
      "url-token": j(te),
      "bad-url-token": j(se),
      "delim-token": j(I),
      "number-token": j(L),
      "percentage-token": j(B),
      "dimension-token": j(z),
      "whitespace-token": j(W),
      "CDO-token": j(Rt),
      "CDC-token": j(ae),
      "colon-token": j(ne),
      "semicolon-token": j(re),
      "comma-token": j(ce),
      "[-token": j(pe),
      "]-token": j(Se),
      "(-token": j(M),
      ")-token": j(E),
      "{-token": j(V),
      "}-token": j(ue)
    }, ql = {
      // token type aliases
      string: j(Te),
      ident: j(y),
      // percentage
      percentage: me(Bl),
      // numeric
      zero: Io(),
      number: me(Wl),
      integer: me(Hl),
      // complex types
      "custom-ident": Rl,
      "dashed-ident": zo,
      "custom-property-name": Ml,
      "hex-color": Nl,
      "id-selector": Dl,
      // element( <id-selector> )
      "an-plus-b": El,
      urange: Pl,
      "declaration-value": jl,
      "any-value": Fl
    };
    function Gl(e) {
      const {
        angle: t,
        decibel: n,
        frequency: r,
        flex: i,
        length: o,
        resolution: s,
        semitones: c,
        time: l
      } = e || {};
      return {
        dimension: me(Ie(null)),
        angle: me(Ie(t)),
        decibel: me(Ie(n)),
        frequency: me(Ie(r)),
        flex: me(Ie(i)),
        length: me(Io(Ie(o))),
        resolution: me(Ie(s)),
        semitones: me(Ie(c)),
        time: me(Ie(l))
      };
    }
    function Vl(e) {
      return _(_(_({}, Ul), ql), Gl(e));
    }
    const Kl = [
      // absolute length units https://www.w3.org/TR/css-values-3/#lengths
      "cm",
      "mm",
      "q",
      "in",
      "pt",
      "pc",
      "px",
      // font-relative length units https://drafts.csswg.org/css-values-4/#font-relative-lengths
      "em",
      "rem",
      "ex",
      "rex",
      "cap",
      "rcap",
      "ch",
      "rch",
      "ic",
      "ric",
      "lh",
      "rlh",
      // viewport-percentage lengths https://drafts.csswg.org/css-values-4/#viewport-relative-lengths
      "vw",
      "svw",
      "lvw",
      "dvw",
      "vh",
      "svh",
      "lvh",
      "dvh",
      "vi",
      "svi",
      "lvi",
      "dvi",
      "vb",
      "svb",
      "lvb",
      "dvb",
      "vmin",
      "svmin",
      "lvmin",
      "dvmin",
      "vmax",
      "svmax",
      "lvmax",
      "dvmax",
      // container relative lengths https://drafts.csswg.org/css-contain-3/#container-lengths
      "cqw",
      "cqh",
      "cqi",
      "cqb",
      "cqmin",
      "cqmax"
    ], Yl = ["deg", "grad", "rad", "turn"], Ql = ["s", "ms"], Xl = ["hz", "khz"], Zl = ["dpi", "dpcm", "dppx", "x"], Jl = ["fr"], ec = ["db"], tc = ["st"], oi = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      angle: Yl,
      decibel: ec,
      flex: Jl,
      frequency: Xl,
      length: Kl,
      resolution: Zl,
      semitones: tc,
      time: Ql
    }, Symbol.toStringTag, { value: "Module" }));
    function nc(e, t, n) {
      return Object.assign(bn("SyntaxError", e), {
        input: t,
        offset: n,
        rawMessage: e,
        message: e + `
  ` + t + `
--` + new Array((n || t.length) + 1).join("-") + "^"
      });
    }
    const rc = 9, ic = 10, oc = 12, sc = 13, ac = 32;
    class lc {
      constructor(t) {
        this.str = t, this.pos = 0;
      }
      charCodeAt(t) {
        return t < this.str.length ? this.str.charCodeAt(t) : 0;
      }
      charCode() {
        return this.charCodeAt(this.pos);
      }
      nextCharCode() {
        return this.charCodeAt(this.pos + 1);
      }
      nextNonWsCode(t) {
        return this.charCodeAt(this.findWsEnd(t));
      }
      skipWs() {
        this.pos = this.findWsEnd(this.pos);
      }
      findWsEnd(t) {
        for (; t < this.str.length; t++) {
          const n = this.str.charCodeAt(t);
          if (n !== sc && n !== ic && n !== oc && n !== ac && n !== rc)
            break;
        }
        return t;
      }
      substringToPos(t) {
        return this.str.substring(this.pos, this.pos = t);
      }
      eat(t) {
        this.charCode() !== t && this.error("Expect `" + String.fromCharCode(t) + "`"), this.pos++;
      }
      peek() {
        return this.pos < this.str.length ? this.str.charAt(this.pos++) : "";
      }
      error(t) {
        throw new nc(t, this.str, this.pos);
      }
    }
    const cc = 9, uc = 10, hc = 12, fc = 13, pc = 32, Ro = 33, Ar = 35, si = 38, sn = 39, Mo = 40, dc = 41, No = 42, Or = 43, Er = 44, ai = 45, Lr = 60, Do = 62, Zn = 63, mc = 64, wn = 91, $r = 93, an = 123, li = 124, ci = 125, ui = 8734, Pt = new Uint8Array(128).map(
      (e, t) => /[a-zA-Z0-9\-]/.test(String.fromCharCode(t)) ? 1 : 0
    ), hi = {
      " ": 1,
      "&&": 2,
      "||": 3,
      "|": 4
    };
    function ln(e) {
      return e.substringToPos(
        e.findWsEnd(e.pos)
      );
    }
    function dt(e) {
      let t = e.pos;
      for (; t < e.str.length; t++) {
        const n = e.str.charCodeAt(t);
        if (n >= 128 || Pt[n] === 0)
          break;
      }
      return e.pos === t && e.error("Expect a keyword"), e.substringToPos(t);
    }
    function cn(e) {
      let t = e.pos;
      for (; t < e.str.length; t++) {
        const n = e.str.charCodeAt(t);
        if (n < 48 || n > 57)
          break;
      }
      return e.pos === t && e.error("Expect a number"), e.substringToPos(t);
    }
    function gc(e) {
      const t = e.str.indexOf("'", e.pos + 1);
      return t === -1 && (e.pos = e.str.length, e.error("Expect an apostrophe")), e.substringToPos(t + 1);
    }
    function fi(e) {
      let t = null, n = null;
      return e.eat(an), e.skipWs(), t = cn(e), e.skipWs(), e.charCode() === Er ? (e.pos++, e.skipWs(), e.charCode() !== ci && (n = cn(e), e.skipWs())) : n = t, e.eat(ci), {
        min: Number(t),
        max: n ? Number(n) : 0
      };
    }
    function bc(e) {
      let t = null, n = !1;
      switch (e.charCode()) {
        case No:
          e.pos++, t = {
            min: 0,
            max: 0
          };
          break;
        case Or:
          e.pos++, t = {
            min: 1,
            max: 0
          };
          break;
        case Zn:
          e.pos++, t = {
            min: 0,
            max: 1
          };
          break;
        case Ar:
          e.pos++, n = !0, e.charCode() === an ? t = fi(e) : e.charCode() === Zn ? (e.pos++, t = {
            min: 0,
            max: 0
          }) : t = {
            min: 1,
            max: 0
          };
          break;
        case an:
          t = fi(e);
          break;
        default:
          return null;
      }
      return {
        type: "Multiplier",
        comma: n,
        min: t.min,
        max: t.max,
        term: null
      };
    }
    function mt(e, t) {
      const n = bc(e);
      return n !== null ? (n.term = t, e.charCode() === Ar && e.charCodeAt(e.pos - 1) === Or ? mt(e, n) : n) : t;
    }
    function Rn(e) {
      const t = e.peek();
      return t === "" ? null : {
        type: "Token",
        value: t
      };
    }
    function yc(e) {
      let t;
      return e.eat(Lr), e.eat(sn), t = dt(e), e.eat(sn), e.eat(Do), mt(e, {
        type: "Property",
        name: t
      });
    }
    function kc(e) {
      let t = null, n = null, r = 1;
      return e.eat(wn), e.charCode() === ai && (e.peek(), r = -1), r == -1 && e.charCode() === ui ? e.peek() : (t = r * Number(cn(e)), Pt[e.charCode()] !== 0 && (t += dt(e))), ln(e), e.eat(Er), ln(e), e.charCode() === ui ? e.peek() : (r = 1, e.charCode() === ai && (e.peek(), r = -1), n = r * Number(cn(e)), Pt[e.charCode()] !== 0 && (n += dt(e))), e.eat($r), {
        type: "Range",
        min: t,
        max: n
      };
    }
    function xc(e) {
      let t, n = null;
      return e.eat(Lr), t = dt(e), e.charCode() === Mo && e.nextCharCode() === dc && (e.pos += 2, t += "()"), e.charCodeAt(e.findWsEnd(e.pos)) === wn && (ln(e), n = kc(e)), e.eat(Do), mt(e, {
        type: "Type",
        name: t,
        opts: n
      });
    }
    function wc(e) {
      const t = dt(e);
      return e.charCode() === Mo ? (e.pos++, {
        type: "Function",
        name: t
      }) : mt(e, {
        type: "Keyword",
        name: t
      });
    }
    function vc(e, t) {
      function n(i, o) {
        return {
          type: "Group",
          terms: i,
          combinator: o,
          disallowEmpty: !1,
          explicit: !1
        };
      }
      let r;
      for (t = Object.keys(t).sort((i, o) => hi[i] - hi[o]); t.length > 0; ) {
        r = t.shift();
        let i = 0, o = 0;
        for (; i < e.length; i++) {
          const s = e[i];
          s.type === "Combinator" && (s.value === r ? (o === -1 && (o = i - 1), e.splice(i, 1), i--) : (o !== -1 && i - o > 1 && (e.splice(
            o,
            i - o,
            n(e.slice(o, i), r)
          ), i = o + 1), o = -1));
        }
        o !== -1 && t.length && e.splice(
          o,
          i - o,
          n(e.slice(o, i), r)
        );
      }
      return r;
    }
    function jo(e) {
      const t = [], n = {};
      let r, i = null, o = e.pos;
      for (; r = Cc(e); )
        r.type !== "Spaces" && (r.type === "Combinator" ? ((i === null || i.type === "Combinator") && (e.pos = o, e.error("Unexpected combinator")), n[r.value] = !0) : i !== null && i.type !== "Combinator" && (n[" "] = !0, t.push({
          type: "Combinator",
          value: " "
        })), t.push(r), i = r, o = e.pos);
      return i !== null && i.type === "Combinator" && (e.pos -= o, e.error("Unexpected combinator")), {
        type: "Group",
        terms: t,
        combinator: vc(t, n) || " ",
        disallowEmpty: !1,
        explicit: !1
      };
    }
    function Sc(e) {
      let t;
      return e.eat(wn), t = jo(e), e.eat($r), t.explicit = !0, e.charCode() === Ro && (e.pos++, t.disallowEmpty = !0), t;
    }
    function Cc(e) {
      let t = e.charCode();
      if (t < 128 && Pt[t] === 1)
        return wc(e);
      switch (t) {
        case $r:
          break;
        case wn:
          return mt(e, Sc(e));
        case Lr:
          return e.nextCharCode() === sn ? yc(e) : xc(e);
        case li:
          return {
            type: "Combinator",
            value: e.substringToPos(
              e.pos + (e.nextCharCode() === li ? 2 : 1)
            )
          };
        case si:
          return e.pos++, e.eat(si), {
            type: "Combinator",
            value: "&&"
          };
        case Er:
          return e.pos++, {
            type: "Comma"
          };
        case sn:
          return mt(e, {
            type: "String",
            value: gc(e)
          });
        case pc:
        case cc:
        case uc:
        case fc:
        case hc:
          return {
            type: "Spaces",
            value: ln(e)
          };
        case mc:
          return t = e.nextCharCode(), t < 128 && Pt[t] === 1 ? (e.pos++, {
            type: "AtKeyword",
            name: dt(e)
          }) : Rn(e);
        case No:
        case Or:
        case Zn:
        case Ar:
        case Ro:
          break;
        case an:
          if (t = e.nextCharCode(), t < 48 || t > 57)
            return Rn(e);
          break;
        default:
          return Rn(e);
      }
    }
    function Fo(e) {
      const t = new lc(e), n = jo(t);
      return t.pos !== e.length && t.error("Unexpected input"), n.terms.length === 1 && n.terms[0].type === "Group" ? n.terms[0] : n;
    }
    const St = function() {
    };
    function pi(e) {
      return typeof e == "function" ? e : St;
    }
    function Tc(e, t, n) {
      function r(s) {
        switch (i.call(n, s), s.type) {
          case "Group":
            s.terms.forEach(r);
            break;
          case "Multiplier":
            r(s.term);
            break;
          case "Type":
          case "Property":
          case "Keyword":
          case "AtKeyword":
          case "Function":
          case "String":
          case "Token":
          case "Comma":
            break;
          default:
            throw new Error("Unknown type: " + s.type);
        }
        o.call(n, s);
      }
      let i = St, o = St;
      if (typeof t == "function" ? i = t : (i = pi(t.enter), o = pi(t.leave)), i === St && o === St)
        throw new Error("Neither `enter` nor `leave` walker handler is set or both aren't a function");
      r(e);
    }
    const Ac = {
      decorator(e) {
        const t = [];
        let n = null;
        return Z(_({}, e), {
          node(r) {
            const i = n;
            n = r, e.node.call(this, r), n = i;
          },
          emit(r, i, o) {
            t.push({
              type: i,
              value: r,
              node: o ? null : n
            });
          },
          result() {
            return t;
          }
        });
      }
    };
    function Oc(e) {
      const t = [];
      return gn(
        e,
        (n, r, i) => t.push({
          type: n,
          value: e.slice(r, i),
          node: null
        })
      ), t;
    }
    function Ec(e, t) {
      return typeof e == "string" ? Oc(e) : t.generate(e, Ac);
    }
    const N = { type: "Match" }, D = { type: "Mismatch" }, _r = { type: "DisallowEmpty" }, Lc = 40, $c = 41;
    function J(e, t, n) {
      return t === N && n === D || e === N && t === N && n === N ? e : (e.type === "If" && e.else === D && t === N && (t = e.then, e = e.match), {
        type: "If",
        match: e,
        then: t,
        else: n
      });
    }
    function Bo(e) {
      return e.length > 2 && e.charCodeAt(e.length - 2) === Lc && e.charCodeAt(e.length - 1) === $c;
    }
    function di(e) {
      return e.type === "Keyword" || e.type === "AtKeyword" || e.type === "Function" || e.type === "Type" && Bo(e.name);
    }
    function Jn(e, t, n) {
      switch (e) {
        case " ": {
          let r = N;
          for (let i = t.length - 1; i >= 0; i--) {
            const o = t[i];
            r = J(
              o,
              r,
              D
            );
          }
          return r;
        }
        case "|": {
          let r = D, i = null;
          for (let o = t.length - 1; o >= 0; o--) {
            let s = t[o];
            if (di(s) && (i === null && o > 0 && di(t[o - 1]) && (i = /* @__PURE__ */ Object.create(null), r = J(
              {
                type: "Enum",
                map: i
              },
              N,
              r
            )), i !== null)) {
              const c = (Bo(s.name) ? s.name.slice(0, -1) : s.name).toLowerCase();
              if (!(c in i)) {
                i[c] = s;
                continue;
              }
            }
            i = null, r = J(
              s,
              N,
              r
            );
          }
          return r;
        }
        case "&&": {
          if (t.length > 5)
            return {
              type: "MatchOnce",
              terms: t,
              all: !0
            };
          let r = D;
          for (let i = t.length - 1; i >= 0; i--) {
            const o = t[i];
            let s;
            t.length > 1 ? s = Jn(
              e,
              t.filter(function(c) {
                return c !== o;
              }),
              !1
            ) : s = N, r = J(
              o,
              s,
              r
            );
          }
          return r;
        }
        case "||": {
          if (t.length > 5)
            return {
              type: "MatchOnce",
              terms: t,
              all: !1
            };
          let r = n ? N : D;
          for (let i = t.length - 1; i >= 0; i--) {
            const o = t[i];
            let s;
            t.length > 1 ? s = Jn(
              e,
              t.filter(function(c) {
                return c !== o;
              }),
              !0
            ) : s = N, r = J(
              o,
              s,
              r
            );
          }
          return r;
        }
      }
    }
    function _c(e) {
      let t = N, n = Pr(e.term);
      if (e.max === 0)
        n = J(
          n,
          _r,
          D
        ), t = J(
          n,
          null,
          // will be a loop
          D
        ), t.then = J(
          N,
          N,
          t
          // make a loop
        ), e.comma && (t.then.else = J(
          { type: "Comma", syntax: e },
          t,
          D
        ));
      else
        for (let r = e.min || 1; r <= e.max; r++)
          e.comma && t !== N && (t = J(
            { type: "Comma", syntax: e },
            t,
            D
          )), t = J(
            n,
            J(
              N,
              N,
              t
            ),
            D
          );
      if (e.min === 0)
        t = J(
          N,
          N,
          t
        );
      else
        for (let r = 0; r < e.min - 1; r++)
          e.comma && t !== N && (t = J(
            { type: "Comma", syntax: e },
            t,
            D
          )), t = J(
            n,
            t,
            D
          );
      return t;
    }
    function Pr(e) {
      if (typeof e == "function")
        return {
          type: "Generic",
          fn: e
        };
      switch (e.type) {
        case "Group": {
          let t = Jn(
            e.combinator,
            e.terms.map(Pr),
            !1
          );
          return e.disallowEmpty && (t = J(
            t,
            _r,
            D
          )), t;
        }
        case "Multiplier":
          return _c(e);
        case "Type":
        case "Property":
          return {
            type: e.type,
            name: e.name,
            syntax: e
          };
        case "Keyword":
          return {
            type: e.type,
            name: e.name.toLowerCase(),
            syntax: e
          };
        case "AtKeyword":
          return {
            type: e.type,
            name: "@" + e.name.toLowerCase(),
            syntax: e
          };
        case "Function":
          return {
            type: e.type,
            name: e.name.toLowerCase() + "(",
            syntax: e
          };
        case "String":
          return e.value.length === 3 ? {
            type: "Token",
            value: e.value.charAt(1),
            syntax: e
          } : {
            type: e.type,
            value: e.value.substr(1, e.value.length - 2).replace(/\\'/g, "'"),
            syntax: e
          };
        case "Token":
          return {
            type: e.type,
            value: e.value,
            syntax: e
          };
        case "Comma":
          return {
            type: e.type,
            syntax: e
          };
        default:
          throw new Error("Unknown node type:", e.type);
      }
    }
    function Vt(e, t) {
      return typeof e == "string" && (e = Fo(e)), {
        type: "MatchGraph",
        match: Pr(e),
        syntax: t || null,
        source: e
      };
    }
    const { hasOwnProperty: mi } = Object.prototype, Pc = 0, zc = 1, er = 2, Wo = 3, gi = "Match", Ic = "Mismatch", Rc = "Maximum iteration number exceeded (please fill an issue on https://github.com/csstree/csstree/issues)", bi = 15e3;
    function Mc(e) {
      let t = null, n = null, r = e;
      for (; r !== null; )
        n = r.prev, r.prev = t, t = r, r = n;
      return t;
    }
    function Mn(e, t) {
      if (e.length !== t.length)
        return !1;
      for (let n = 0; n < e.length; n++) {
        const r = t.charCodeAt(n);
        let i = e.charCodeAt(n);
        if (i >= 65 && i <= 90 && (i = i | 32), i !== r)
          return !1;
      }
      return !0;
    }
    function Nc(e) {
      return e.type !== I ? !1 : e.value !== "?";
    }
    function yi(e) {
      return e === null ? !0 : e.type === ce || e.type === $ || e.type === M || e.type === pe || e.type === V || Nc(e);
    }
    function ki(e) {
      return e === null ? !0 : e.type === E || e.type === Se || e.type === ue || e.type === I && e.value === "/";
    }
    function Dc(e, t, n) {
      function r() {
        do
          x++, b = x < e.length ? e[x] : null;
        while (b !== null && (b.type === W || b.type === X));
      }
      function i(A) {
        const P = x + A;
        return P < e.length ? e[P] : null;
      }
      function o(A, P) {
        return {
          nextState: A,
          matchStack: v,
          syntaxStack: h,
          thenStack: d,
          tokenIndex: x,
          prev: P
        };
      }
      function s(A) {
        d = {
          nextState: A,
          matchStack: v,
          syntaxStack: h,
          prev: d
        };
      }
      function c(A) {
        m = o(A, m);
      }
      function l() {
        v = {
          type: zc,
          syntax: t.syntax,
          token: b,
          prev: v
        }, r(), w = null, x > T && (T = x);
      }
      function a() {
        h = {
          syntax: t.syntax,
          opts: t.syntax.opts || h !== null && h.opts || null,
          prev: h
        }, v = {
          type: er,
          syntax: t.syntax,
          token: v.token,
          prev: v
        };
      }
      function u() {
        v.type === er ? v = v.prev : v = {
          type: Wo,
          syntax: h.syntax,
          token: v.token,
          prev: v
        }, h = h.prev;
      }
      let h = null, d = null, m = null, w = null, k = 0, C = null, b = null, x = -1, T = 0, v = {
        type: Pc,
        syntax: null,
        token: null,
        prev: null
      };
      for (r(); C === null && ++k < bi; )
        switch (t.type) {
          case "Match":
            if (d === null) {
              if (b !== null && (x !== e.length - 1 || b.value !== "\\0" && b.value !== "\\9")) {
                t = D;
                break;
              }
              C = gi;
              break;
            }
            if (t = d.nextState, t === _r)
              if (d.matchStack === v) {
                t = D;
                break;
              } else
                t = N;
            for (; d.syntaxStack !== h; )
              u();
            d = d.prev;
            break;
          case "Mismatch":
            if (w !== null && w !== !1)
              (m === null || x > m.tokenIndex) && (m = w, w = !1);
            else if (m === null) {
              C = Ic;
              break;
            }
            t = m.nextState, d = m.thenStack, h = m.syntaxStack, v = m.matchStack, x = m.tokenIndex, b = x < e.length ? e[x] : null, m = m.prev;
            break;
          case "MatchGraph":
            t = t.match;
            break;
          case "If":
            t.else !== D && c(t.else), t.then !== N && s(t.then), t = t.match;
            break;
          case "MatchOnce":
            t = {
              type: "MatchOnceBuffer",
              syntax: t,
              index: 0,
              mask: 0
            };
            break;
          case "MatchOnceBuffer": {
            const O = t.syntax.terms;
            if (t.index === O.length) {
              if (t.mask === 0 || t.syntax.all) {
                t = D;
                break;
              }
              t = N;
              break;
            }
            if (t.mask === (1 << O.length) - 1) {
              t = N;
              break;
            }
            for (; t.index < O.length; t.index++) {
              const f = 1 << t.index;
              if (!(t.mask & f)) {
                c(t), s({
                  type: "AddMatchOnce",
                  syntax: t.syntax,
                  mask: t.mask | f
                }), t = O[t.index++];
                break;
              }
            }
            break;
          }
          case "AddMatchOnce":
            t = {
              type: "MatchOnceBuffer",
              syntax: t.syntax,
              index: 0,
              mask: t.mask
            };
            break;
          case "Enum":
            if (b !== null) {
              let O = b.value.toLowerCase();
              if (O.indexOf("\\") !== -1 && (O = O.replace(/\\[09].*$/, "")), mi.call(t.map, O)) {
                t = t.map[O];
                break;
              }
            }
            t = D;
            break;
          case "Generic": {
            const O = h !== null ? h.opts : null, f = x + Math.floor(t.fn(b, i, O));
            if (!isNaN(f) && f > x) {
              for (; x < f; )
                l();
              t = N;
            } else
              t = D;
            break;
          }
          case "Type":
          case "Property": {
            const O = t.type === "Type" ? "types" : "properties", f = mi.call(n, O) ? n[O][t.name] : null;
            if (!f || !f.match)
              throw new Error(
                "Bad syntax reference: " + (t.type === "Type" ? "<" + t.name + ">" : "<'" + t.name + "'>")
              );
            if (w !== !1 && b !== null && t.type === "Type" && // https://drafts.csswg.org/css-values-4/#custom-idents
            // When parsing positionally-ambiguous keywords in a property value, a <custom-ident> production
            // can only claim the keyword if no other unfulfilled production can claim it.
            (t.name === "custom-ident" && b.type === y || // https://drafts.csswg.org/css-values-4/#lengths
            // ... if a `0` could be parsed as either a <number> or a <length> in a property (such as line-height),
            // it must parse as a <number>
            t.name === "length" && b.value === "0")) {
              w === null && (w = o(t, m)), t = D;
              break;
            }
            a(), t = f.matchRef || f.match;
            break;
          }
          case "Keyword": {
            const O = t.name;
            if (b !== null) {
              let f = b.value;
              if (f.indexOf("\\") !== -1 && (f = f.replace(/\\[09].*$/, "")), Mn(f, O)) {
                l(), t = N;
                break;
              }
            }
            t = D;
            break;
          }
          case "AtKeyword":
          case "Function":
            if (b !== null && Mn(b.value, t.name)) {
              l(), t = N;
              break;
            }
            t = D;
            break;
          case "Token":
            if (b !== null && b.value === t.value) {
              l(), t = N;
              break;
            }
            t = D;
            break;
          case "Comma":
            b !== null && b.type === ce ? yi(v.token) ? t = D : (l(), t = ki(b) ? D : N) : t = yi(v.token) || ki(b) ? N : D;
            break;
          case "String":
            let A = "", P = x;
            for (; P < e.length && A.length < t.value.length; P++)
              A += e[P].value;
            if (Mn(A, t.value)) {
              for (; x < P; )
                l();
              t = N;
            } else
              t = D;
            break;
          default:
            throw new Error("Unknown node type: " + t.type);
        }
      switch (C) {
        case null:
          console.warn("[csstree-match] BREAK after " + bi + " iterations"), C = Rc, v = null;
          break;
        case gi:
          for (; h !== null; )
            u();
          break;
        default:
          v = null;
      }
      return {
        tokens: e,
        reason: C,
        iterations: k,
        match: v,
        longestMatch: T
      };
    }
    function xi(e, t, n) {
      const r = Dc(e, t, n || {});
      if (r.match === null)
        return r;
      let i = r.match, o = r.match = {
        syntax: t.syntax || null,
        match: []
      };
      const s = [o];
      for (i = Mc(i).prev; i !== null; ) {
        switch (i.type) {
          case er:
            o.match.push(o = {
              syntax: i.syntax,
              match: []
            }), s.push(o);
            break;
          case Wo:
            s.pop(), o = s[s.length - 1];
            break;
          default:
            o.match.push({
              syntax: i.syntax || null,
              token: i.token.value,
              node: i.token.node
            });
        }
        i = i.prev;
      }
      return r;
    }
    function Ho(e) {
      function t(i) {
        return i === null ? !1 : i.type === "Type" || i.type === "Property" || i.type === "Keyword";
      }
      function n(i) {
        if (Array.isArray(i.match)) {
          for (let o = 0; o < i.match.length; o++)
            if (n(i.match[o]))
              return t(i.syntax) && r.unshift(i.syntax), !0;
        } else if (i.node === e)
          return r = t(i.syntax) ? [i.syntax] : [], !0;
        return !1;
      }
      let r = null;
      return this.matched !== null && n(this.matched), r;
    }
    function jc(e, t) {
      return zr(this, e, (n) => n.type === "Type" && n.name === t);
    }
    function Fc(e, t) {
      return zr(this, e, (n) => n.type === "Property" && n.name === t);
    }
    function Bc(e) {
      return zr(this, e, (t) => t.type === "Keyword");
    }
    function zr(e, t, n) {
      const r = Ho.call(e, t);
      return r === null ? !1 : r.some(n);
    }
    const Wc = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      getTrace: Ho,
      isKeyword: Bc,
      isProperty: Fc,
      isType: jc
    }, Symbol.toStringTag, { value: "Module" }));
    function Uo(e) {
      return "node" in e ? e.node : Uo(e.match[0]);
    }
    function qo(e) {
      return "node" in e ? e.node : qo(e.match[e.match.length - 1]);
    }
    function wi(e, t, n, r, i) {
      function o(c) {
        if (c.syntax !== null && c.syntax.type === r && c.syntax.name === i) {
          const l = Uo(c), a = qo(c);
          e.syntax.walk(t, function(u, h, d) {
            if (u === l) {
              const m = new q();
              do {
                if (m.appendData(h.data), h.data === a)
                  break;
                h = h.next;
              } while (h !== null);
              s.push({
                parent: d,
                nodes: m
              });
            }
          });
        }
        Array.isArray(c.match) && c.match.forEach(o);
      }
      const s = [];
      return n.matched !== null && o(n.matched), s;
    }
    const { hasOwnProperty: At } = Object.prototype;
    function Nn(e) {
      return typeof e == "number" && isFinite(e) && Math.floor(e) === e && e >= 0;
    }
    function vi(e) {
      return !!e && Nn(e.offset) && Nn(e.line) && Nn(e.column);
    }
    function Hc(e, t) {
      return function(r, i) {
        if (!r || r.constructor !== Object)
          return i(r, "Type of node should be an Object");
        for (let o in r) {
          let s = !0;
          if (At.call(r, o) !== !1) {
            if (o === "type")
              r.type !== e && i(r, "Wrong node type `" + r.type + "`, expected `" + e + "`");
            else if (o === "loc") {
              if (r.loc === null)
                continue;
              if (r.loc && r.loc.constructor === Object)
                if (typeof r.loc.source != "string")
                  o += ".source";
                else if (!vi(r.loc.start))
                  o += ".start";
                else if (!vi(r.loc.end))
                  o += ".end";
                else
                  continue;
              s = !1;
            } else if (t.hasOwnProperty(o)) {
              s = !1;
              for (let c = 0; !s && c < t[o].length; c++) {
                const l = t[o][c];
                switch (l) {
                  case String:
                    s = typeof r[o] == "string";
                    break;
                  case Boolean:
                    s = typeof r[o] == "boolean";
                    break;
                  case null:
                    s = r[o] === null;
                    break;
                  default:
                    typeof l == "string" ? s = r[o] && r[o].type === l : Array.isArray(l) && (s = r[o] instanceof q);
                }
              }
            } else
              i(r, "Unknown field `" + o + "` for " + e + " node type");
            s || i(r, "Bad value for `" + e + "." + o + "`");
          }
        }
        for (const o in t)
          At.call(t, o) && At.call(r, o) === !1 && i(r, "Field `" + e + "." + o + "` is missed");
      };
    }
    function Go(e, t) {
      const n = [];
      for (let r = 0; r < e.length; r++) {
        const i = e[r];
        if (i === String || i === Boolean)
          n.push(i.name.toLowerCase());
        else if (i === null)
          n.push("null");
        else if (typeof i == "string")
          n.push(i);
        else if (Array.isArray(i))
          n.push("List<" + (Go(i, t) || "any") + ">");
        else
          throw new Error("Wrong value `" + i + "` in `" + t + "` structure definition");
      }
      return n.join(" | ");
    }
    function Uc(e, t) {
      const n = t.structure, r = {
        type: String,
        loc: !0
      }, i = {
        type: '"' + e + '"'
      };
      for (const o in n) {
        if (At.call(n, o) === !1)
          continue;
        const s = r[o] = Array.isArray(n[o]) ? n[o].slice() : [n[o]];
        i[o] = Go(s, e + "." + o);
      }
      return {
        docs: i,
        check: Hc(e, r)
      };
    }
    function qc(e) {
      const t = {};
      if (e.node) {
        for (const n in e.node)
          if (At.call(e.node, n)) {
            const r = e.node[n];
            if (r.structure)
              t[n] = Uc(n, r);
            else
              throw new Error("Missed `structure` field in `" + n + "` node type definition");
          }
      }
      return t;
    }
    const Gc = Vt(Oo.join(" | "));
    function tr(e, t, n) {
      const r = {};
      for (const i in e)
        e[i].syntax && (r[i] = n ? e[i].syntax : Sr(e[i].syntax, { compact: t }));
      return r;
    }
    function Vc(e, t, n) {
      const r = {};
      for (const [i, o] of Object.entries(e))
        r[i] = {
          prelude: o.prelude && (n ? o.prelude.syntax : Sr(o.prelude.syntax, { compact: t })),
          descriptors: o.descriptors && tr(o.descriptors, t, n)
        };
      return r;
    }
    function Kc(e) {
      for (let t = 0; t < e.length; t++)
        if (e[t].value.toLowerCase() === "var(")
          return !0;
      return !1;
    }
    function Yc(e) {
      const t = e.terms[0];
      return e.explicit === !1 && e.terms.length === 1 && t.type === "Multiplier" && t.comma === !0;
    }
    function ye(e, t, n) {
      return _({
        matched: e,
        iterations: n,
        error: t
      }, Wc);
    }
    function ot(e, t, n, r) {
      const i = Ec(n, e.syntax);
      let o;
      return Kc(i) ? ye(null, new Error("Matching for a tree with var() is not supported")) : (r && (o = xi(i, e.cssWideKeywordsSyntax, e)), (!r || !o.match) && (o = xi(i, t.match, e), !o.match) ? ye(
        null,
        new Cl(o.reason, t.syntax, n, o),
        o.iterations
      ) : ye(o.match, null, o.iterations));
    }
    class Si {
      constructor(t, n, r) {
        if (this.cssWideKeywordsSyntax = Gc, this.syntax = n, this.generic = !1, this.units = _({}, oi), this.atrules = /* @__PURE__ */ Object.create(null), this.properties = /* @__PURE__ */ Object.create(null), this.types = /* @__PURE__ */ Object.create(null), this.structure = r || qc(t), t) {
          if (t.units)
            for (const i of Object.keys(oi))
              Array.isArray(t.units[i]) && (this.units[i] = t.units[i]);
          if (t.types)
            for (const [i, o] of Object.entries(t.types))
              this.addType_(i, o);
          if (t.generic) {
            this.generic = !0;
            for (const [i, o] of Object.entries(Vl(this.units)))
              this.addType_(i, o);
          }
          if (t.atrules)
            for (const [i, o] of Object.entries(t.atrules))
              this.addAtrule_(i, o);
          if (t.properties)
            for (const [i, o] of Object.entries(t.properties))
              this.addProperty_(i, o);
        }
      }
      checkStructure(t) {
        function n(o, s) {
          i.push({ node: o, message: s });
        }
        const r = this.structure, i = [];
        return this.syntax.walk(t, function(o) {
          r.hasOwnProperty(o.type) ? r[o.type].check(o, n) : n(o, "Unknown node type `" + o.type + "`");
        }), i.length ? i : !1;
      }
      createDescriptor(t, n, r, i = null) {
        const o = {
          type: n,
          name: r
        }, s = {
          type: n,
          name: r,
          parent: i,
          serializable: typeof t == "string" || t && typeof t.type == "string",
          syntax: null,
          match: null,
          matchRef: null
          // used for properties when a syntax referenced as <'property'> in other syntax definitions
        };
        return typeof t == "function" ? s.match = Vt(t, o) : (typeof t == "string" ? Object.defineProperty(s, "syntax", {
          get() {
            return Object.defineProperty(s, "syntax", {
              value: Fo(t)
            }), s.syntax;
          }
        }) : s.syntax = t, Object.defineProperty(s, "match", {
          get() {
            return Object.defineProperty(s, "match", {
              value: Vt(s.syntax, o)
            }), s.match;
          }
        }), n === "Property" && Object.defineProperty(s, "matchRef", {
          get() {
            const c = s.syntax, l = Yc(c) ? Vt(Z(_({}, c), {
              terms: [c.terms[0].term]
            }), o) : null;
            return Object.defineProperty(s, "matchRef", {
              value: l
            }), l;
          }
        })), s;
      }
      addAtrule_(t, n) {
        n && (this.atrules[t] = {
          type: "Atrule",
          name: t,
          prelude: n.prelude ? this.createDescriptor(n.prelude, "AtrulePrelude", t) : null,
          descriptors: n.descriptors ? Object.keys(n.descriptors).reduce(
            (r, i) => (r[i] = this.createDescriptor(n.descriptors[i], "AtruleDescriptor", i, t), r),
            /* @__PURE__ */ Object.create(null)
          ) : null
        });
      }
      addProperty_(t, n) {
        n && (this.properties[t] = this.createDescriptor(n, "Property", t));
      }
      addType_(t, n) {
        n && (this.types[t] = this.createDescriptor(n, "Type", t));
      }
      checkAtruleName(t) {
        if (!this.getAtrule(t))
          return new kt("Unknown at-rule", "@" + t);
      }
      checkAtrulePrelude(t, n) {
        const r = this.checkAtruleName(t);
        if (r)
          return r;
        const i = this.getAtrule(t);
        if (!i.prelude && n)
          return new SyntaxError("At-rule `@" + t + "` should not contain a prelude");
        if (i.prelude && !n && !ot(this, i.prelude, "", !1).matched)
          return new SyntaxError("At-rule `@" + t + "` should contain a prelude");
      }
      checkAtruleDescriptorName(t, n) {
        const r = this.checkAtruleName(t);
        if (r)
          return r;
        const i = this.getAtrule(t), o = Pn(n);
        if (!i.descriptors)
          return new SyntaxError("At-rule `@" + t + "` has no known descriptors");
        if (!i.descriptors[o.name] && !i.descriptors[o.basename])
          return new kt("Unknown at-rule descriptor", n);
      }
      checkPropertyName(t) {
        if (!this.getProperty(t))
          return new kt("Unknown property", t);
      }
      matchAtrulePrelude(t, n) {
        const r = this.checkAtrulePrelude(t, n);
        if (r)
          return ye(null, r);
        const i = this.getAtrule(t);
        return i.prelude ? ot(this, i.prelude, n || "", !1) : ye(null, null);
      }
      matchAtruleDescriptor(t, n, r) {
        const i = this.checkAtruleDescriptorName(t, n);
        if (i)
          return ye(null, i);
        const o = this.getAtrule(t), s = Pn(n);
        return ot(this, o.descriptors[s.name] || o.descriptors[s.basename], r, !1);
      }
      matchDeclaration(t) {
        return t.type !== "Declaration" ? ye(null, new Error("Not a Declaration node")) : this.matchProperty(t.property, t.value);
      }
      matchProperty(t, n) {
        if (ii(t).custom)
          return ye(null, new Error("Lexer matching doesn't applicable for custom properties"));
        const r = this.checkPropertyName(t);
        return r ? ye(null, r) : ot(this, this.getProperty(t), n, !0);
      }
      matchType(t, n) {
        const r = this.getType(t);
        return r ? ot(this, r, n, !1) : ye(null, new kt("Unknown type", t));
      }
      match(t, n) {
        return typeof t != "string" && (!t || !t.type) ? ye(null, new kt("Bad syntax")) : ((typeof t == "string" || !t.match) && (t = this.createDescriptor(t, "Type", "anonymous")), ot(this, t, n, !1));
      }
      findValueFragments(t, n, r, i) {
        return wi(this, n, this.matchProperty(t, n), r, i);
      }
      findDeclarationValueFragments(t, n, r) {
        return wi(this, t.value, this.matchDeclaration(t), n, r);
      }
      findAllFragments(t, n, r) {
        const i = [];
        return this.syntax.walk(t, {
          visit: "Declaration",
          enter: (o) => {
            i.push.apply(i, this.findDeclarationValueFragments(o, n, r));
          }
        }), i;
      }
      getAtrule(t, n = !0) {
        const r = Pn(t);
        return (r.vendor && n ? this.atrules[r.name] || this.atrules[r.basename] : this.atrules[r.name]) || null;
      }
      getAtrulePrelude(t, n = !0) {
        const r = this.getAtrule(t, n);
        return r && r.prelude || null;
      }
      getAtruleDescriptor(t, n) {
        return this.atrules.hasOwnProperty(t) && this.atrules.declarators && this.atrules[t].declarators[n] || null;
      }
      getProperty(t, n = !0) {
        const r = ii(t);
        return (r.vendor && n ? this.properties[r.name] || this.properties[r.basename] : this.properties[r.name]) || null;
      }
      getType(t) {
        return hasOwnProperty.call(this.types, t) ? this.types[t] : null;
      }
      validate() {
        function t(i, o, s, c) {
          if (s.has(o))
            return s.get(o);
          s.set(o, !1), c.syntax !== null && Tc(c.syntax, function(l) {
            if (l.type !== "Type" && l.type !== "Property")
              return;
            const a = l.type === "Type" ? i.types : i.properties, u = l.type === "Type" ? n : r;
            (!hasOwnProperty.call(a, l.name) || t(i, l.name, u, a[l.name])) && s.set(o, !0);
          }, this);
        }
        let n = /* @__PURE__ */ new Map(), r = /* @__PURE__ */ new Map();
        for (const i in this.types)
          t(this, i, n, this.types[i]);
        for (const i in this.properties)
          t(this, i, r, this.properties[i]);
        return n = [...n.keys()].filter((i) => n.get(i)), r = [...r.keys()].filter((i) => r.get(i)), n.length || r.length ? {
          types: n,
          properties: r
        } : null;
      }
      dump(t, n) {
        return {
          generic: this.generic,
          units: this.units,
          types: tr(this.types, !n, t),
          properties: tr(this.properties, !n, t),
          atrules: Vc(this.atrules, !n, t)
        };
      }
      toString() {
        return JSON.stringify(this.dump());
      }
    }
    function Dn(e, t) {
      return typeof t == "string" && /^\s*\|/.test(t) ? typeof e == "string" ? e + t : t.replace(/^\s*\|\s*/, "") : t || null;
    }
    function Ci(e, t) {
      const n = /* @__PURE__ */ Object.create(null);
      for (const [r, i] of Object.entries(e))
        if (i) {
          n[r] = {};
          for (const o of Object.keys(i))
            t.includes(o) && (n[r][o] = i[o]);
        }
      return n;
    }
    function nr(e, t) {
      const n = _({}, e);
      for (const [r, i] of Object.entries(t))
        switch (r) {
          case "generic":
            n[r] = !!i;
            break;
          case "units":
            n[r] = _({}, e[r]);
            for (const [o, s] of Object.entries(i))
              n[r][o] = Array.isArray(s) ? s : [];
            break;
          case "atrules":
            n[r] = _({}, e[r]);
            for (const [o, s] of Object.entries(i)) {
              const c = n[r][o] || {}, l = n[r][o] = {
                prelude: c.prelude || null,
                descriptors: _({}, c.descriptors)
              };
              if (s) {
                l.prelude = s.prelude ? Dn(l.prelude, s.prelude) : l.prelude || null;
                for (const [a, u] of Object.entries(s.descriptors || {}))
                  l.descriptors[a] = u ? Dn(l.descriptors[a], u) : null;
                Object.keys(l.descriptors).length || (l.descriptors = null);
              }
            }
            break;
          case "types":
          case "properties":
            n[r] = _({}, e[r]);
            for (const [o, s] of Object.entries(i))
              n[r][o] = Dn(n[r][o], s);
            break;
          case "scope":
          case "features":
            n[r] = _({}, e[r]);
            for (const [o, s] of Object.entries(i))
              n[r][o] = _(_({}, n[r][o]), s);
            break;
          case "parseContext":
            n[r] = _(_({}, e[r]), i);
            break;
          case "atrule":
          case "pseudo":
            n[r] = _(_({}, e[r]), Ci(i, ["parse"]));
            break;
          case "node":
            n[r] = _(_({}, e[r]), Ci(i, ["name", "structure", "parse", "generate", "walkContext"]));
            break;
        }
      return n;
    }
    function Vo(e) {
      const t = el(e), n = yl(e), r = dl(e), { fromPlainObject: i, toPlainObject: o } = ml(n), s = {
        lexer: null,
        createLexer: (c) => new Si(c, s, s.lexer.structure),
        tokenize: gn,
        parse: t,
        generate: r,
        walk: n,
        find: n.find,
        findLast: n.findLast,
        findAll: n.findAll,
        fromPlainObject: i,
        toPlainObject: o,
        fork(c) {
          const l = nr({}, e);
          return Vo(
            typeof c == "function" ? c(l, Object.assign) : nr(l, c)
          );
        }
      };
      return s.lexer = new Si({
        generic: e.generic,
        units: e.units,
        types: e.types,
        atrules: e.atrules,
        properties: e.properties,
        node: e.node
      }, s), s;
    }
    const Qc = (e) => Vo(nr({}, e)), Xc = {
      generic: !0,
      units: {
        angle: [
          "deg",
          "grad",
          "rad",
          "turn"
        ],
        decibel: [
          "db"
        ],
        flex: [
          "fr"
        ],
        frequency: [
          "hz",
          "khz"
        ],
        length: [
          "cm",
          "mm",
          "q",
          "in",
          "pt",
          "pc",
          "px",
          "em",
          "rem",
          "ex",
          "rex",
          "cap",
          "rcap",
          "ch",
          "rch",
          "ic",
          "ric",
          "lh",
          "rlh",
          "vw",
          "svw",
          "lvw",
          "dvw",
          "vh",
          "svh",
          "lvh",
          "dvh",
          "vi",
          "svi",
          "lvi",
          "dvi",
          "vb",
          "svb",
          "lvb",
          "dvb",
          "vmin",
          "svmin",
          "lvmin",
          "dvmin",
          "vmax",
          "svmax",
          "lvmax",
          "dvmax",
          "cqw",
          "cqh",
          "cqi",
          "cqb",
          "cqmin",
          "cqmax"
        ],
        resolution: [
          "dpi",
          "dpcm",
          "dppx",
          "x"
        ],
        semitones: [
          "st"
        ],
        time: [
          "s",
          "ms"
        ]
      },
      types: {
        "abs()": "abs( <calc-sum> )",
        "absolute-size": "xx-small|x-small|small|medium|large|x-large|xx-large|xxx-large",
        "acos()": "acos( <calc-sum> )",
        "alpha-value": "<number>|<percentage>",
        "angle-percentage": "<angle>|<percentage>",
        "angular-color-hint": "<angle-percentage>",
        "angular-color-stop": "<color>&&<color-stop-angle>?",
        "angular-color-stop-list": "[<angular-color-stop> [, <angular-color-hint>]?]# , <angular-color-stop>",
        "animateable-feature": "scroll-position|contents|<custom-ident>",
        "asin()": "asin( <calc-sum> )",
        "atan()": "atan( <calc-sum> )",
        "atan2()": "atan2( <calc-sum> , <calc-sum> )",
        attachment: "scroll|fixed|local",
        "attr()": "attr( <attr-name> <type-or-unit>? [, <attr-fallback>]? )",
        "attr-matcher": "['~'|'|'|'^'|'$'|'*']? '='",
        "attr-modifier": "i|s",
        "attribute-selector": "'[' <wq-name> ']'|'[' <wq-name> <attr-matcher> [<string-token>|<ident-token>] <attr-modifier>? ']'",
        "auto-repeat": "repeat( [auto-fill|auto-fit] , [<line-names>? <fixed-size>]+ <line-names>? )",
        "auto-track-list": "[<line-names>? [<fixed-size>|<fixed-repeat>]]* <line-names>? <auto-repeat> [<line-names>? [<fixed-size>|<fixed-repeat>]]* <line-names>?",
        axis: "block|inline|vertical|horizontal",
        "baseline-position": "[first|last]? baseline",
        "basic-shape": "<inset()>|<circle()>|<ellipse()>|<polygon()>|<path()>",
        "bg-image": "none|<image>",
        "bg-layer": "<bg-image>||<bg-position> [/ <bg-size>]?||<repeat-style>||<attachment>||<box>||<box>",
        "bg-position": "[[left|center|right|top|bottom|<length-percentage>]|[left|center|right|<length-percentage>] [top|center|bottom|<length-percentage>]|[center|[left|right] <length-percentage>?]&&[center|[top|bottom] <length-percentage>?]]",
        "bg-size": "[<length-percentage>|auto]{1,2}|cover|contain",
        "blur()": "blur( <length> )",
        "blend-mode": "normal|multiply|screen|overlay|darken|lighten|color-dodge|color-burn|hard-light|soft-light|difference|exclusion|hue|saturation|color|luminosity",
        box: "border-box|padding-box|content-box",
        "brightness()": "brightness( <number-percentage> )",
        "calc()": "calc( <calc-sum> )",
        "calc-sum": "<calc-product> [['+'|'-'] <calc-product>]*",
        "calc-product": "<calc-value> ['*' <calc-value>|'/' <number>]*",
        "calc-value": "<number>|<dimension>|<percentage>|<calc-constant>|( <calc-sum> )",
        "calc-constant": "e|pi|infinity|-infinity|NaN",
        "cf-final-image": "<image>|<color>",
        "cf-mixing-image": "<percentage>?&&<image>",
        "circle()": "circle( [<shape-radius>]? [at <position>]? )",
        "clamp()": "clamp( <calc-sum>#{3} )",
        "class-selector": "'.' <ident-token>",
        "clip-source": "<url>",
        color: "<color-base>|currentColor|<system-color>|<device-cmyk()>|<light-dark()>|<-non-standard-color>",
        "color-stop": "<color-stop-length>|<color-stop-angle>",
        "color-stop-angle": "<angle-percentage>{1,2}",
        "color-stop-length": "<length-percentage>{1,2}",
        "color-stop-list": "[<linear-color-stop> [, <linear-color-hint>]?]# , <linear-color-stop>",
        combinator: "'>'|'+'|'~'|['|' '|']",
        "common-lig-values": "[common-ligatures|no-common-ligatures]",
        "compat-auto": "searchfield|textarea|push-button|slider-horizontal|checkbox|radio|square-button|menulist|listbox|meter|progress-bar|button",
        "composite-style": "clear|copy|source-over|source-in|source-out|source-atop|destination-over|destination-in|destination-out|destination-atop|xor",
        "compositing-operator": "add|subtract|intersect|exclude",
        "compound-selector": "[<type-selector>? <subclass-selector>*]!",
        "compound-selector-list": "<compound-selector>#",
        "complex-selector": "<complex-selector-unit> [<combinator>? <complex-selector-unit>]*",
        "complex-selector-list": "<complex-selector>#",
        "conic-gradient()": "conic-gradient( [from <angle>]? [at <position>]? , <angular-color-stop-list> )",
        "contextual-alt-values": "[contextual|no-contextual]",
        "content-distribution": "space-between|space-around|space-evenly|stretch",
        "content-list": "[<string>|contents|<image>|<counter>|<quote>|<target>|<leader()>|<attr()>]+",
        "content-position": "center|start|end|flex-start|flex-end",
        "content-replacement": "<image>",
        "contrast()": "contrast( [<number-percentage>] )",
        "cos()": "cos( <calc-sum> )",
        counter: "<counter()>|<counters()>",
        "counter()": "counter( <counter-name> , <counter-style>? )",
        "counter-name": "<custom-ident>",
        "counter-style": "<counter-style-name>|symbols( )",
        "counter-style-name": "<custom-ident>",
        "counters()": "counters( <counter-name> , <string> , <counter-style>? )",
        "cross-fade()": "cross-fade( <cf-mixing-image> , <cf-final-image>? )",
        "cubic-bezier-timing-function": "ease|ease-in|ease-out|ease-in-out|cubic-bezier( <number [0,1]> , <number> , <number [0,1]> , <number> )",
        "deprecated-system-color": "ActiveBorder|ActiveCaption|AppWorkspace|Background|ButtonFace|ButtonHighlight|ButtonShadow|ButtonText|CaptionText|GrayText|Highlight|HighlightText|InactiveBorder|InactiveCaption|InactiveCaptionText|InfoBackground|InfoText|Menu|MenuText|Scrollbar|ThreeDDarkShadow|ThreeDFace|ThreeDHighlight|ThreeDLightShadow|ThreeDShadow|Window|WindowFrame|WindowText",
        "discretionary-lig-values": "[discretionary-ligatures|no-discretionary-ligatures]",
        "display-box": "contents|none",
        "display-inside": "flow|flow-root|table|flex|grid|ruby",
        "display-internal": "table-row-group|table-header-group|table-footer-group|table-row|table-cell|table-column-group|table-column|table-caption|ruby-base|ruby-text|ruby-base-container|ruby-text-container",
        "display-legacy": "inline-block|inline-list-item|inline-table|inline-flex|inline-grid",
        "display-listitem": "<display-outside>?&&[flow|flow-root]?&&list-item",
        "display-outside": "block|inline|run-in",
        "drop-shadow()": "drop-shadow( <length>{2,3} <color>? )",
        "east-asian-variant-values": "[jis78|jis83|jis90|jis04|simplified|traditional]",
        "east-asian-width-values": "[full-width|proportional-width]",
        "element()": "element( <custom-ident> , [first|start|last|first-except]? )|element( <id-selector> )",
        "ellipse()": "ellipse( [<shape-radius>{2}]? [at <position>]? )",
        "ending-shape": "circle|ellipse",
        "env()": "env( <custom-ident> , <declaration-value>? )",
        "exp()": "exp( <calc-sum> )",
        "explicit-track-list": "[<line-names>? <track-size>]+ <line-names>?",
        "family-name": "<string>|<custom-ident>+",
        "feature-tag-value": "<string> [<integer>|on|off]?",
        "feature-type": "@stylistic|@historical-forms|@styleset|@character-variant|@swash|@ornaments|@annotation",
        "feature-value-block": "<feature-type> '{' <feature-value-declaration-list> '}'",
        "feature-value-block-list": "<feature-value-block>+",
        "feature-value-declaration": "<custom-ident> : <integer>+ ;",
        "feature-value-declaration-list": "<feature-value-declaration>",
        "feature-value-name": "<custom-ident>",
        "fill-rule": "nonzero|evenodd",
        "filter-function": "<blur()>|<brightness()>|<contrast()>|<drop-shadow()>|<grayscale()>|<hue-rotate()>|<invert()>|<opacity()>|<saturate()>|<sepia()>",
        "filter-function-list": "[<filter-function>|<url>]+",
        "final-bg-layer": "<'background-color'>||<bg-image>||<bg-position> [/ <bg-size>]?||<repeat-style>||<attachment>||<box>||<box>",
        "fixed-breadth": "<length-percentage>",
        "fixed-repeat": "repeat( [<integer [1,∞]>] , [<line-names>? <fixed-size>]+ <line-names>? )",
        "fixed-size": "<fixed-breadth>|minmax( <fixed-breadth> , <track-breadth> )|minmax( <inflexible-breadth> , <fixed-breadth> )",
        "font-stretch-absolute": "normal|ultra-condensed|extra-condensed|condensed|semi-condensed|semi-expanded|expanded|extra-expanded|ultra-expanded|<percentage>",
        "font-variant-css21": "[normal|small-caps]",
        "font-weight-absolute": "normal|bold|<number [1,1000]>",
        "frequency-percentage": "<frequency>|<percentage>",
        "general-enclosed": "[<function-token> <any-value>? )]|[( <any-value>? )]",
        "generic-family": "<generic-script-specific>|<generic-complete>|<generic-incomplete>|<-non-standard-generic-family>",
        "generic-name": "serif|sans-serif|cursive|fantasy|monospace",
        "geometry-box": "<shape-box>|fill-box|stroke-box|view-box",
        gradient: "<linear-gradient()>|<repeating-linear-gradient()>|<radial-gradient()>|<repeating-radial-gradient()>|<conic-gradient()>|<repeating-conic-gradient()>|<-legacy-gradient>",
        "grayscale()": "grayscale( <number-percentage> )",
        "grid-line": "auto|<custom-ident>|[<integer>&&<custom-ident>?]|[span&&[<integer>||<custom-ident>]]",
        "historical-lig-values": "[historical-ligatures|no-historical-ligatures]",
        "hsl()": "hsl( <hue> <percentage> <percentage> [/ <alpha-value>]? )|hsl( <hue> , <percentage> , <percentage> , <alpha-value>? )",
        "hsla()": "hsla( <hue> <percentage> <percentage> [/ <alpha-value>]? )|hsla( <hue> , <percentage> , <percentage> , <alpha-value>? )",
        hue: "<number>|<angle>",
        "hue-rotate()": "hue-rotate( <angle> )",
        "hwb()": "hwb( [<hue>|none] [<percentage>|none] [<percentage>|none] [/ [<alpha-value>|none]]? )",
        "hypot()": "hypot( <calc-sum># )",
        image: "<url>|<image()>|<image-set()>|<element()>|<paint()>|<cross-fade()>|<gradient>",
        "image()": "image( <image-tags>? [<image-src>? , <color>?]! )",
        "image-set()": "image-set( <image-set-option># )",
        "image-set-option": "[<image>|<string>] [<resolution>||type( <string> )]",
        "image-src": "<url>|<string>",
        "image-tags": "ltr|rtl",
        "inflexible-breadth": "<length-percentage>|min-content|max-content|auto",
        "inset()": "inset( <length-percentage>{1,4} [round <'border-radius'>]? )",
        "invert()": "invert( <number-percentage> )",
        "keyframes-name": "<custom-ident>|<string>",
        "keyframe-block": "<keyframe-selector># { <declaration-list> }",
        "keyframe-block-list": "<keyframe-block>+",
        "keyframe-selector": "from|to|<percentage>|<timeline-range-name> <percentage>",
        "lab()": "lab( [<percentage>|<number>|none] [<percentage>|<number>|none] [<percentage>|<number>|none] [/ [<alpha-value>|none]]? )",
        "layer()": "layer( <layer-name> )",
        "layer-name": "<ident> ['.' <ident>]*",
        "lch()": "lch( [<percentage>|<number>|none] [<percentage>|<number>|none] [<hue>|none] [/ [<alpha-value>|none]]? )",
        "leader()": "leader( <leader-type> )",
        "leader-type": "dotted|solid|space|<string>",
        "length-percentage": "<length>|<percentage>",
        "light-dark()": "light-dark( <color> , <color> )",
        "line-names": "'[' <custom-ident>* ']'",
        "line-name-list": "[<line-names>|<name-repeat>]+",
        "line-style": "none|hidden|dotted|dashed|solid|double|groove|ridge|inset|outset",
        "line-width": "<length>|thin|medium|thick",
        "linear-color-hint": "<length-percentage>",
        "linear-color-stop": "<color> <color-stop-length>?",
        "linear-gradient()": "linear-gradient( [<angle>|to <side-or-corner>]? , <color-stop-list> )",
        "log()": "log( <calc-sum> , <calc-sum>? )",
        "mask-layer": "<mask-reference>||<position> [/ <bg-size>]?||<repeat-style>||<geometry-box>||[<geometry-box>|no-clip]||<compositing-operator>||<masking-mode>",
        "mask-position": "[<length-percentage>|left|center|right] [<length-percentage>|top|center|bottom]?",
        "mask-reference": "none|<image>|<mask-source>",
        "mask-source": "<url>",
        "masking-mode": "alpha|luminance|match-source",
        "matrix()": "matrix( <number>#{6} )",
        "matrix3d()": "matrix3d( <number>#{16} )",
        "max()": "max( <calc-sum># )",
        "media-and": "<media-in-parens> [and <media-in-parens>]+",
        "media-condition": "<media-not>|<media-and>|<media-or>|<media-in-parens>",
        "media-condition-without-or": "<media-not>|<media-and>|<media-in-parens>",
        "media-feature": "( [<mf-plain>|<mf-boolean>|<mf-range>] )",
        "media-in-parens": "( <media-condition> )|<media-feature>|<general-enclosed>",
        "media-not": "not <media-in-parens>",
        "media-or": "<media-in-parens> [or <media-in-parens>]+",
        "media-query": "<media-condition>|[not|only]? <media-type> [and <media-condition-without-or>]?",
        "media-query-list": "<media-query>#",
        "media-type": "<ident>",
        "mf-boolean": "<mf-name>",
        "mf-name": "<ident>",
        "mf-plain": "<mf-name> : <mf-value>",
        "mf-range": "<mf-name> ['<'|'>']? '='? <mf-value>|<mf-value> ['<'|'>']? '='? <mf-name>|<mf-value> '<' '='? <mf-name> '<' '='? <mf-value>|<mf-value> '>' '='? <mf-name> '>' '='? <mf-value>",
        "mf-value": "<number>|<dimension>|<ident>|<ratio>",
        "min()": "min( <calc-sum># )",
        "minmax()": "minmax( [<length-percentage>|min-content|max-content|auto] , [<length-percentage>|<flex>|min-content|max-content|auto] )",
        "mod()": "mod( <calc-sum> , <calc-sum> )",
        "name-repeat": "repeat( [<integer [1,∞]>|auto-fill] , <line-names>+ )",
        "named-color": "transparent|aliceblue|antiquewhite|aqua|aquamarine|azure|beige|bisque|black|blanchedalmond|blue|blueviolet|brown|burlywood|cadetblue|chartreuse|chocolate|coral|cornflowerblue|cornsilk|crimson|cyan|darkblue|darkcyan|darkgoldenrod|darkgray|darkgreen|darkgrey|darkkhaki|darkmagenta|darkolivegreen|darkorange|darkorchid|darkred|darksalmon|darkseagreen|darkslateblue|darkslategray|darkslategrey|darkturquoise|darkviolet|deeppink|deepskyblue|dimgray|dimgrey|dodgerblue|firebrick|floralwhite|forestgreen|fuchsia|gainsboro|ghostwhite|gold|goldenrod|gray|green|greenyellow|grey|honeydew|hotpink|indianred|indigo|ivory|khaki|lavender|lavenderblush|lawngreen|lemonchiffon|lightblue|lightcoral|lightcyan|lightgoldenrodyellow|lightgray|lightgreen|lightgrey|lightpink|lightsalmon|lightseagreen|lightskyblue|lightslategray|lightslategrey|lightsteelblue|lightyellow|lime|limegreen|linen|magenta|maroon|mediumaquamarine|mediumblue|mediumorchid|mediumpurple|mediumseagreen|mediumslateblue|mediumspringgreen|mediumturquoise|mediumvioletred|midnightblue|mintcream|mistyrose|moccasin|navajowhite|navy|oldlace|olive|olivedrab|orange|orangered|orchid|palegoldenrod|palegreen|paleturquoise|palevioletred|papayawhip|peachpuff|peru|pink|plum|powderblue|purple|rebeccapurple|red|rosybrown|royalblue|saddlebrown|salmon|sandybrown|seagreen|seashell|sienna|silver|skyblue|slateblue|slategray|slategrey|snow|springgreen|steelblue|tan|teal|thistle|tomato|turquoise|violet|wheat|white|whitesmoke|yellow|yellowgreen",
        "namespace-prefix": "<ident>",
        "ns-prefix": "[<ident-token>|'*']? '|'",
        "number-percentage": "<number>|<percentage>",
        "numeric-figure-values": "[lining-nums|oldstyle-nums]",
        "numeric-fraction-values": "[diagonal-fractions|stacked-fractions]",
        "numeric-spacing-values": "[proportional-nums|tabular-nums]",
        nth: "<an-plus-b>|even|odd",
        "opacity()": "opacity( [<number-percentage>] )",
        "overflow-position": "unsafe|safe",
        "outline-radius": "<length>|<percentage>",
        "page-body": "<declaration>? [; <page-body>]?|<page-margin-box> <page-body>",
        "page-margin-box": "<page-margin-box-type> '{' <declaration-list> '}'",
        "page-margin-box-type": "@top-left-corner|@top-left|@top-center|@top-right|@top-right-corner|@bottom-left-corner|@bottom-left|@bottom-center|@bottom-right|@bottom-right-corner|@left-top|@left-middle|@left-bottom|@right-top|@right-middle|@right-bottom",
        "page-selector-list": "[<page-selector>#]?",
        "page-selector": "<pseudo-page>+|<ident> <pseudo-page>*",
        "page-size": "A5|A4|A3|B5|B4|JIS-B5|JIS-B4|letter|legal|ledger",
        "path()": "path( [<fill-rule> ,]? <string> )",
        "paint()": "paint( <ident> , <declaration-value>? )",
        "perspective()": "perspective( [<length [0,∞]>|none] )",
        "polygon()": "polygon( <fill-rule>? , [<length-percentage> <length-percentage>]# )",
        position: "[[left|center|right]||[top|center|bottom]|[left|center|right|<length-percentage>] [top|center|bottom|<length-percentage>]?|[[left|right] <length-percentage>]&&[[top|bottom] <length-percentage>]]",
        "pow()": "pow( <calc-sum> , <calc-sum> )",
        "pseudo-class-selector": "':' <ident-token>|':' <function-token> <any-value> ')'",
        "pseudo-element-selector": "':' <pseudo-class-selector>|<legacy-pseudo-element-selector>",
        "pseudo-page": ": [left|right|first|blank]",
        quote: "open-quote|close-quote|no-open-quote|no-close-quote",
        "radial-gradient()": "radial-gradient( [<ending-shape>||<size>]? [at <position>]? , <color-stop-list> )",
        ratio: "<number [0,∞]> [/ <number [0,∞]>]?",
        "ray()": "ray( <angle>&&<ray-size>?&&contain?&&[at <position>]? )",
        "ray-size": "closest-side|closest-corner|farthest-side|farthest-corner|sides",
        "relative-selector": "<combinator>? <complex-selector>",
        "relative-selector-list": "<relative-selector>#",
        "relative-size": "larger|smaller",
        "rem()": "rem( <calc-sum> , <calc-sum> )",
        "repeat-style": "repeat-x|repeat-y|[repeat|space|round|no-repeat]{1,2}",
        "repeating-conic-gradient()": "repeating-conic-gradient( [from <angle>]? [at <position>]? , <angular-color-stop-list> )",
        "repeating-linear-gradient()": "repeating-linear-gradient( [<angle>|to <side-or-corner>]? , <color-stop-list> )",
        "repeating-radial-gradient()": "repeating-radial-gradient( [<ending-shape>||<size>]? [at <position>]? , <color-stop-list> )",
        "reversed-counter-name": "reversed( <counter-name> )",
        "rgb()": "rgb( <percentage>{3} [/ <alpha-value>]? )|rgb( <number>{3} [/ <alpha-value>]? )|rgb( <percentage>#{3} , <alpha-value>? )|rgb( <number>#{3} , <alpha-value>? )",
        "rgba()": "rgba( <percentage>{3} [/ <alpha-value>]? )|rgba( <number>{3} [/ <alpha-value>]? )|rgba( <percentage>#{3} , <alpha-value>? )|rgba( <number>#{3} , <alpha-value>? )",
        "rotate()": "rotate( [<angle>|<zero>] )",
        "rotate3d()": "rotate3d( <number> , <number> , <number> , [<angle>|<zero>] )",
        "rotateX()": "rotateX( [<angle>|<zero>] )",
        "rotateY()": "rotateY( [<angle>|<zero>] )",
        "rotateZ()": "rotateZ( [<angle>|<zero>] )",
        "round()": "round( <rounding-strategy>? , <calc-sum> , <calc-sum> )",
        "rounding-strategy": "nearest|up|down|to-zero",
        "saturate()": "saturate( <number-percentage> )",
        "scale()": "scale( [<number>|<percentage>]#{1,2} )",
        "scale3d()": "scale3d( [<number>|<percentage>]#{3} )",
        "scaleX()": "scaleX( [<number>|<percentage>] )",
        "scaleY()": "scaleY( [<number>|<percentage>] )",
        "scaleZ()": "scaleZ( [<number>|<percentage>] )",
        "scroll()": "scroll( [<axis>||<scroller>]? )",
        scroller: "root|nearest",
        "self-position": "center|start|end|self-start|self-end|flex-start|flex-end",
        "shape-radius": "<length-percentage>|closest-side|farthest-side",
        "sign()": "sign( <calc-sum> )",
        "skew()": "skew( [<angle>|<zero>] , [<angle>|<zero>]? )",
        "skewX()": "skewX( [<angle>|<zero>] )",
        "skewY()": "skewY( [<angle>|<zero>] )",
        "sepia()": "sepia( <number-percentage> )",
        shadow: "inset?&&<length>{2,4}&&<color>?",
        "shadow-t": "[<length>{2,3}&&<color>?]",
        shape: "rect( <top> , <right> , <bottom> , <left> )|rect( <top> <right> <bottom> <left> )",
        "shape-box": "<box>|margin-box",
        "side-or-corner": "[left|right]||[top|bottom]",
        "sin()": "sin( <calc-sum> )",
        "single-animation": "<'animation-duration'>||<easing-function>||<'animation-delay'>||<single-animation-iteration-count>||<single-animation-direction>||<single-animation-fill-mode>||<single-animation-play-state>||[none|<keyframes-name>]||<single-animation-timeline>",
        "single-animation-direction": "normal|reverse|alternate|alternate-reverse",
        "single-animation-fill-mode": "none|forwards|backwards|both",
        "single-animation-iteration-count": "infinite|<number>",
        "single-animation-play-state": "running|paused",
        "single-animation-timeline": "auto|none|<dashed-ident>|<scroll()>|<view()>",
        "single-transition": "[none|<single-transition-property>]||<time>||<easing-function>||<time>||<transition-behavior-value>",
        "single-transition-property": "all|<custom-ident>",
        size: "closest-side|farthest-side|closest-corner|farthest-corner|<length>|<length-percentage>{2}",
        "sqrt()": "sqrt( <calc-sum> )",
        "step-position": "jump-start|jump-end|jump-none|jump-both|start|end",
        "step-timing-function": "step-start|step-end|steps( <integer> [, <step-position>]? )",
        "subclass-selector": "<id-selector>|<class-selector>|<attribute-selector>|<pseudo-class-selector>",
        "supports-condition": "not <supports-in-parens>|<supports-in-parens> [and <supports-in-parens>]*|<supports-in-parens> [or <supports-in-parens>]*",
        "supports-in-parens": "( <supports-condition> )|<supports-feature>|<general-enclosed>",
        "supports-feature": "<supports-decl>|<supports-selector-fn>",
        "supports-decl": "( <declaration> )",
        "supports-selector-fn": "selector( <complex-selector> )",
        symbol: "<string>|<image>|<custom-ident>",
        "tan()": "tan( <calc-sum> )",
        target: "<target-counter()>|<target-counters()>|<target-text()>",
        "target-counter()": "target-counter( [<string>|<url>] , <custom-ident> , <counter-style>? )",
        "target-counters()": "target-counters( [<string>|<url>] , <custom-ident> , <string> , <counter-style>? )",
        "target-text()": "target-text( [<string>|<url>] , [content|before|after|first-letter]? )",
        "time-percentage": "<time>|<percentage>",
        "timeline-range-name": "cover|contain|entry|exit|entry-crossing|exit-crossing",
        "easing-function": "linear|<cubic-bezier-timing-function>|<step-timing-function>",
        "track-breadth": "<length-percentage>|<flex>|min-content|max-content|auto",
        "track-list": "[<line-names>? [<track-size>|<track-repeat>]]+ <line-names>?",
        "track-repeat": "repeat( [<integer [1,∞]>] , [<line-names>? <track-size>]+ <line-names>? )",
        "track-size": "<track-breadth>|minmax( <inflexible-breadth> , <track-breadth> )|fit-content( <length-percentage> )",
        "transform-function": "<matrix()>|<translate()>|<translateX()>|<translateY()>|<scale()>|<scaleX()>|<scaleY()>|<rotate()>|<skew()>|<skewX()>|<skewY()>|<matrix3d()>|<translate3d()>|<translateZ()>|<scale3d()>|<scaleZ()>|<rotate3d()>|<rotateX()>|<rotateY()>|<rotateZ()>|<perspective()>",
        "transform-list": "<transform-function>+",
        "transition-behavior-value": "normal|allow-discrete",
        "translate()": "translate( <length-percentage> , <length-percentage>? )",
        "translate3d()": "translate3d( <length-percentage> , <length-percentage> , <length> )",
        "translateX()": "translateX( <length-percentage> )",
        "translateY()": "translateY( <length-percentage> )",
        "translateZ()": "translateZ( <length> )",
        "type-or-unit": "string|color|url|integer|number|length|angle|time|frequency|cap|ch|em|ex|ic|lh|rlh|rem|vb|vi|vw|vh|vmin|vmax|mm|Q|cm|in|pt|pc|px|deg|grad|rad|turn|ms|s|Hz|kHz|%",
        "type-selector": "<wq-name>|<ns-prefix>? '*'",
        "var()": "var( <custom-property-name> , <declaration-value>? )",
        "view()": "view( [<axis>||<'view-timeline-inset'>]? )",
        "viewport-length": "auto|<length-percentage>",
        "visual-box": "content-box|padding-box|border-box",
        "wq-name": "<ns-prefix>? <ident-token>",
        "-legacy-gradient": "<-webkit-gradient()>|<-legacy-linear-gradient>|<-legacy-repeating-linear-gradient>|<-legacy-radial-gradient>|<-legacy-repeating-radial-gradient>",
        "-legacy-linear-gradient": "-moz-linear-gradient( <-legacy-linear-gradient-arguments> )|-webkit-linear-gradient( <-legacy-linear-gradient-arguments> )|-o-linear-gradient( <-legacy-linear-gradient-arguments> )",
        "-legacy-repeating-linear-gradient": "-moz-repeating-linear-gradient( <-legacy-linear-gradient-arguments> )|-webkit-repeating-linear-gradient( <-legacy-linear-gradient-arguments> )|-o-repeating-linear-gradient( <-legacy-linear-gradient-arguments> )",
        "-legacy-linear-gradient-arguments": "[<angle>|<side-or-corner>]? , <color-stop-list>",
        "-legacy-radial-gradient": "-moz-radial-gradient( <-legacy-radial-gradient-arguments> )|-webkit-radial-gradient( <-legacy-radial-gradient-arguments> )|-o-radial-gradient( <-legacy-radial-gradient-arguments> )",
        "-legacy-repeating-radial-gradient": "-moz-repeating-radial-gradient( <-legacy-radial-gradient-arguments> )|-webkit-repeating-radial-gradient( <-legacy-radial-gradient-arguments> )|-o-repeating-radial-gradient( <-legacy-radial-gradient-arguments> )",
        "-legacy-radial-gradient-arguments": "[<position> ,]? [[[<-legacy-radial-gradient-shape>||<-legacy-radial-gradient-size>]|[<length>|<percentage>]{2}] ,]? <color-stop-list>",
        "-legacy-radial-gradient-size": "closest-side|closest-corner|farthest-side|farthest-corner|contain|cover",
        "-legacy-radial-gradient-shape": "circle|ellipse",
        "-non-standard-font": "-apple-system-body|-apple-system-headline|-apple-system-subheadline|-apple-system-caption1|-apple-system-caption2|-apple-system-footnote|-apple-system-short-body|-apple-system-short-headline|-apple-system-short-subheadline|-apple-system-short-caption1|-apple-system-short-footnote|-apple-system-tall-body",
        "-non-standard-color": "-moz-ButtonDefault|-moz-ButtonHoverFace|-moz-ButtonHoverText|-moz-CellHighlight|-moz-CellHighlightText|-moz-Combobox|-moz-ComboboxText|-moz-Dialog|-moz-DialogText|-moz-dragtargetzone|-moz-EvenTreeRow|-moz-Field|-moz-FieldText|-moz-html-CellHighlight|-moz-html-CellHighlightText|-moz-mac-accentdarkestshadow|-moz-mac-accentdarkshadow|-moz-mac-accentface|-moz-mac-accentlightesthighlight|-moz-mac-accentlightshadow|-moz-mac-accentregularhighlight|-moz-mac-accentregularshadow|-moz-mac-chrome-active|-moz-mac-chrome-inactive|-moz-mac-focusring|-moz-mac-menuselect|-moz-mac-menushadow|-moz-mac-menutextselect|-moz-MenuHover|-moz-MenuHoverText|-moz-MenuBarText|-moz-MenuBarHoverText|-moz-nativehyperlinktext|-moz-OddTreeRow|-moz-win-communicationstext|-moz-win-mediatext|-moz-activehyperlinktext|-moz-default-background-color|-moz-default-color|-moz-hyperlinktext|-moz-visitedhyperlinktext|-webkit-activelink|-webkit-focus-ring-color|-webkit-link|-webkit-text",
        "-non-standard-image-rendering": "optimize-contrast|-moz-crisp-edges|-o-crisp-edges|-webkit-optimize-contrast",
        "-non-standard-overflow": "overlay|-moz-scrollbars-none|-moz-scrollbars-horizontal|-moz-scrollbars-vertical|-moz-hidden-unscrollable",
        "-non-standard-size": "intrinsic|min-intrinsic|-webkit-fill-available|-webkit-fit-content|-webkit-min-content|-webkit-max-content|-moz-available|-moz-fit-content|-moz-min-content|-moz-max-content",
        "-webkit-gradient()": "-webkit-gradient( <-webkit-gradient-type> , <-webkit-gradient-point> [, <-webkit-gradient-point>|, <-webkit-gradient-radius> , <-webkit-gradient-point>] [, <-webkit-gradient-radius>]? [, <-webkit-gradient-color-stop>]* )",
        "-webkit-gradient-color-stop": "from( <color> )|color-stop( [<number-zero-one>|<percentage>] , <color> )|to( <color> )",
        "-webkit-gradient-point": "[left|center|right|<length-percentage>] [top|center|bottom|<length-percentage>]",
        "-webkit-gradient-radius": "<length>|<percentage>",
        "-webkit-gradient-type": "linear|radial",
        "-webkit-mask-box-repeat": "repeat|stretch|round",
        "-ms-filter-function-list": "<-ms-filter-function>+",
        "-ms-filter-function": "<-ms-filter-function-progid>|<-ms-filter-function-legacy>",
        "-ms-filter-function-progid": "'progid:' [<ident-token> '.']* [<ident-token>|<function-token> <any-value>? )]",
        "-ms-filter-function-legacy": "<ident-token>|<function-token> <any-value>? )",
        "absolute-color-base": "<hex-color>|<absolute-color-function>|<named-color>|transparent",
        "absolute-color-function": "rgb( ) >|<rgba()>|<hsl()>|<hsla()>|<hwb()>|<lab()>|<lch()>|<oklab()>|<oklch()>|<color()>",
        age: "child|young|old",
        "attr-name": "<wq-name>",
        "attr-fallback": "<any-value>",
        "bg-clip": "<box>|border|text",
        bottom: "<length>|auto",
        "container-name": "<custom-ident>",
        "container-condition": "not <query-in-parens>|<query-in-parens> [[and <query-in-parens>]*|[or <query-in-parens>]*]",
        "coord-box": "content-box|padding-box|border-box|fill-box|stroke-box|view-box",
        "generic-voice": "[<age>? <gender> <integer>?]",
        gender: "male|female|neutral",
        "generic-script-specific": "generic( kai )|generic( fangsong )|generic( nastaliq )",
        "generic-complete": "serif|sans-serif|system-ui|cursive|fantasy|math|monospace",
        "generic-incomplete": "ui-serif|ui-sans-serif|ui-monospace|ui-rounded",
        "-non-standard-generic-family": "-apple-system|BlinkMacSystemFont",
        left: "<length>|auto",
        "color-base": "<hex-color>|<color-function>|<named-color>|<color-mix()>|transparent",
        "color-function": "<rgb()>|<rgba()>|<hsl()>|<hsla()>|<hwb()>|<lab()>|<lch()>|<oklab()>|<oklch()>|<color()>",
        "system-color": "AccentColor|AccentColorText|ActiveText|ButtonBorder|ButtonFace|ButtonText|Canvas|CanvasText|Field|FieldText|GrayText|Highlight|HighlightText|LinkText|Mark|MarkText|SelectedItem|SelectedItemText|VisitedText",
        "device-cmyk()": "<legacy-device-cmyk-syntax>|<modern-device-cmyk-syntax>",
        "legacy-device-cmyk-syntax": "device-cmyk( <number>#{4} )",
        "modern-device-cmyk-syntax": "device-cmyk( <cmyk-component>{4} [/ [<alpha-value>|none]]? )",
        "cmyk-component": "<number>|<percentage>|none",
        "color-mix()": "color-mix( <color-interpolation-method> , [<color>&&<percentage [0,100]>?]#{2} )",
        "color-interpolation-method": "in [<rectangular-color-space>|<polar-color-space> <hue-interpolation-method>?|<custom-color-space>]",
        "color-space": "<rectangular-color-space>|<polar-color-space>|<custom-color-space>",
        "rectangular-color-space": "srgb|srgb-linear|display-p3|a98-rgb|prophoto-rgb|rec2020|lab|oklab|xyz|xyz-d50|xyz-d65",
        "polar-color-space": "hsl|hwb|lch|oklch",
        "custom-color-space": "<dashed-ident>",
        "hue-interpolation-method": "[shorter|longer|increasing|decreasing] hue",
        paint: "none|<color>|<url> [none|<color>]?|context-fill|context-stroke",
        "palette-identifier": "<dashed-ident>",
        right: "<length>|auto",
        "scope-start": "<forgiving-selector-list>",
        "scope-end": "<forgiving-selector-list>",
        "forgiving-selector-list": "<complex-real-selector-list>",
        "forgiving-relative-selector-list": "<relative-real-selector-list>",
        "selector-list": "<complex-selector-list>",
        "complex-real-selector-list": "<complex-real-selector>#",
        "simple-selector-list": "<simple-selector>#",
        "relative-real-selector-list": "<relative-real-selector>#",
        "complex-selector-unit": "[<compound-selector>? <pseudo-compound-selector>*]!",
        "complex-real-selector": "<compound-selector> [<combinator>? <compound-selector>]*",
        "relative-real-selector": "<combinator>? <complex-real-selector>",
        "pseudo-compound-selector": "<pseudo-element-selector> <pseudo-class-selector>*",
        "simple-selector": "<type-selector>|<subclass-selector>",
        "legacy-pseudo-element-selector": "':' [before|after|first-line|first-letter]",
        "single-animation-composition": "replace|add|accumulate",
        "svg-length": "<percentage>|<length>|<number>",
        "svg-writing-mode": "lr-tb|rl-tb|tb-rl|lr|rl|tb",
        top: "<length>|auto",
        x: "<number>",
        y: "<number>",
        declaration: "<ident-token> : <declaration-value>? ['!' important]?",
        "declaration-list": "[<declaration>? ';']* <declaration>?",
        url: "url( <string> <url-modifier>* )|<url-token>",
        "url-modifier": "<ident>|<function-token> <any-value> )",
        "number-zero-one": "<number [0,1]>",
        "number-one-or-greater": "<number [1,∞]>",
        "color()": "color( <colorspace-params> [/ [<alpha-value>|none]]? )",
        "colorspace-params": "[<predefined-rgb-params>|<xyz-params>]",
        "predefined-rgb-params": "<predefined-rgb> [<number>|<percentage>|none]{3}",
        "predefined-rgb": "srgb|srgb-linear|display-p3|a98-rgb|prophoto-rgb|rec2020",
        "xyz-params": "<xyz-space> [<number>|<percentage>|none]{3}",
        "xyz-space": "xyz|xyz-d50|xyz-d65",
        "oklab()": "oklab( [<percentage>|<number>|none] [<percentage>|<number>|none] [<percentage>|<number>|none] [/ [<alpha-value>|none]]? )",
        "oklch()": "oklch( [<percentage>|<number>|none] [<percentage>|<number>|none] [<hue>|none] [/ [<alpha-value>|none]]? )",
        "offset-path": "<ray()>|<url>|<basic-shape>",
        "query-in-parens": "( <container-condition> )|( <size-feature> )|style( <style-query> )|<general-enclosed>",
        "size-feature": "<mf-plain>|<mf-boolean>|<mf-range>",
        "style-feature": "<declaration>",
        "style-query": "<style-condition>|<style-feature>",
        "style-condition": "not <style-in-parens>|<style-in-parens> [[and <style-in-parens>]*|[or <style-in-parens>]*]",
        "style-in-parens": "( <style-condition> )|( <style-feature> )|<general-enclosed>",
        "-non-standard-display": "-ms-inline-flexbox|-ms-grid|-ms-inline-grid|-webkit-flex|-webkit-inline-flex|-webkit-box|-webkit-inline-box|-moz-inline-stack|-moz-box|-moz-inline-box",
        "inset-area": "[[left|center|right|span-left|span-right|x-start|x-end|span-x-start|span-x-end|x-self-start|x-self-end|span-x-self-start|span-x-self-end|span-all]||[top|center|bottom|span-top|span-bottom|y-start|y-end|span-y-start|span-y-end|y-self-start|y-self-end|span-y-self-start|span-y-self-end|span-all]|[block-start|center|block-end|span-block-start|span-block-end|span-all]||[inline-start|center|inline-end|span-inline-start|span-inline-end|span-all]|[self-block-start|self-block-end|span-self-block-start|span-self-block-end|span-all]||[self-inline-start|self-inline-end|span-self-inline-start|span-self-inline-end|span-all]|[start|center|end|span-start|span-end|span-all]{1,2}|[self-start|center|self-end|span-self-start|span-self-end|span-all]{1,2}]",
        "position-area": "[[left|center|right|span-left|span-right|x-start|x-end|span-x-start|span-x-end|x-self-start|x-self-end|span-x-self-start|span-x-self-end|span-all]||[top|center|bottom|span-top|span-bottom|y-start|y-end|span-y-start|span-y-end|y-self-start|y-self-end|span-y-self-start|span-y-self-end|span-all]|[block-start|center|block-end|span-block-start|span-block-end|span-all]||[inline-start|center|inline-end|span-inline-start|span-inline-end|span-all]|[self-block-start|center|self-block-end|span-self-block-start|span-self-block-end|span-all]||[self-inline-start|center|self-inline-end|span-self-inline-start|span-self-inline-end|span-all]|[start|center|end|span-start|span-end|span-all]{1,2}|[self-start|center|self-end|span-self-start|span-self-end|span-all]{1,2}]",
        "anchor()": "anchor( <anchor-element>?&&<anchor-side> , <length-percentage>? )",
        "anchor-side": "inside|outside|top|left|right|bottom|start|end|self-start|self-end|<percentage>|center",
        "anchor-size()": "anchor-size( [<anchor-element>||<anchor-size>]? , <length-percentage>? )",
        "anchor-size": "width|height|block|inline|self-block|self-inline",
        "anchor-element": "<dashed-ident>",
        "try-size": "most-width|most-height|most-block-size|most-inline-size",
        "try-tactic": "flip-block||flip-inline||flip-start",
        "font-variant-css2": "normal|small-caps",
        "font-width-css3": "normal|ultra-condensed|extra-condensed|condensed|semi-condensed|semi-expanded|expanded|extra-expanded|ultra-expanded",
        "system-family-name": "caption|icon|menu|message-box|small-caption|status-bar"
      },
      properties: {
        "--*": "<declaration-value>",
        "-ms-accelerator": "false|true",
        "-ms-block-progression": "tb|rl|bt|lr",
        "-ms-content-zoom-chaining": "none|chained",
        "-ms-content-zooming": "none|zoom",
        "-ms-content-zoom-limit": "<'-ms-content-zoom-limit-min'> <'-ms-content-zoom-limit-max'>",
        "-ms-content-zoom-limit-max": "<percentage>",
        "-ms-content-zoom-limit-min": "<percentage>",
        "-ms-content-zoom-snap": "<'-ms-content-zoom-snap-type'>||<'-ms-content-zoom-snap-points'>",
        "-ms-content-zoom-snap-points": "snapInterval( <percentage> , <percentage> )|snapList( <percentage># )",
        "-ms-content-zoom-snap-type": "none|proximity|mandatory",
        "-ms-filter": "<string>",
        "-ms-flow-from": "[none|<custom-ident>]#",
        "-ms-flow-into": "[none|<custom-ident>]#",
        "-ms-grid-columns": "none|<track-list>|<auto-track-list>",
        "-ms-grid-rows": "none|<track-list>|<auto-track-list>",
        "-ms-high-contrast-adjust": "auto|none",
        "-ms-hyphenate-limit-chars": "auto|<integer>{1,3}",
        "-ms-hyphenate-limit-lines": "no-limit|<integer>",
        "-ms-hyphenate-limit-zone": "<percentage>|<length>",
        "-ms-ime-align": "auto|after",
        "-ms-overflow-style": "auto|none|scrollbar|-ms-autohiding-scrollbar",
        "-ms-scrollbar-3dlight-color": "<color>",
        "-ms-scrollbar-arrow-color": "<color>",
        "-ms-scrollbar-base-color": "<color>",
        "-ms-scrollbar-darkshadow-color": "<color>",
        "-ms-scrollbar-face-color": "<color>",
        "-ms-scrollbar-highlight-color": "<color>",
        "-ms-scrollbar-shadow-color": "<color>",
        "-ms-scrollbar-track-color": "<color>",
        "-ms-scroll-chaining": "chained|none",
        "-ms-scroll-limit": "<'-ms-scroll-limit-x-min'> <'-ms-scroll-limit-y-min'> <'-ms-scroll-limit-x-max'> <'-ms-scroll-limit-y-max'>",
        "-ms-scroll-limit-x-max": "auto|<length>",
        "-ms-scroll-limit-x-min": "<length>",
        "-ms-scroll-limit-y-max": "auto|<length>",
        "-ms-scroll-limit-y-min": "<length>",
        "-ms-scroll-rails": "none|railed",
        "-ms-scroll-snap-points-x": "snapInterval( <length-percentage> , <length-percentage> )|snapList( <length-percentage># )",
        "-ms-scroll-snap-points-y": "snapInterval( <length-percentage> , <length-percentage> )|snapList( <length-percentage># )",
        "-ms-scroll-snap-type": "none|proximity|mandatory",
        "-ms-scroll-snap-x": "<'-ms-scroll-snap-type'> <'-ms-scroll-snap-points-x'>",
        "-ms-scroll-snap-y": "<'-ms-scroll-snap-type'> <'-ms-scroll-snap-points-y'>",
        "-ms-scroll-translation": "none|vertical-to-horizontal",
        "-ms-text-autospace": "none|ideograph-alpha|ideograph-numeric|ideograph-parenthesis|ideograph-space",
        "-ms-touch-select": "grippers|none",
        "-ms-user-select": "none|element|text",
        "-ms-wrap-flow": "auto|both|start|end|maximum|clear",
        "-ms-wrap-margin": "<length>",
        "-ms-wrap-through": "wrap|none",
        "-moz-appearance": "none|button|button-arrow-down|button-arrow-next|button-arrow-previous|button-arrow-up|button-bevel|button-focus|caret|checkbox|checkbox-container|checkbox-label|checkmenuitem|dualbutton|groupbox|listbox|listitem|menuarrow|menubar|menucheckbox|menuimage|menuitem|menuitemtext|menulist|menulist-button|menulist-text|menulist-textfield|menupopup|menuradio|menuseparator|meterbar|meterchunk|progressbar|progressbar-vertical|progresschunk|progresschunk-vertical|radio|radio-container|radio-label|radiomenuitem|range|range-thumb|resizer|resizerpanel|scale-horizontal|scalethumbend|scalethumb-horizontal|scalethumbstart|scalethumbtick|scalethumb-vertical|scale-vertical|scrollbarbutton-down|scrollbarbutton-left|scrollbarbutton-right|scrollbarbutton-up|scrollbarthumb-horizontal|scrollbarthumb-vertical|scrollbartrack-horizontal|scrollbartrack-vertical|searchfield|separator|sheet|spinner|spinner-downbutton|spinner-textfield|spinner-upbutton|splitter|statusbar|statusbarpanel|tab|tabpanel|tabpanels|tab-scroll-arrow-back|tab-scroll-arrow-forward|textfield|textfield-multiline|toolbar|toolbarbutton|toolbarbutton-dropdown|toolbargripper|toolbox|tooltip|treeheader|treeheadercell|treeheadersortarrow|treeitem|treeline|treetwisty|treetwistyopen|treeview|-moz-mac-unified-toolbar|-moz-win-borderless-glass|-moz-win-browsertabbar-toolbox|-moz-win-communicationstext|-moz-win-communications-toolbox|-moz-win-exclude-glass|-moz-win-glass|-moz-win-mediatext|-moz-win-media-toolbox|-moz-window-button-box|-moz-window-button-box-maximized|-moz-window-button-close|-moz-window-button-maximize|-moz-window-button-minimize|-moz-window-button-restore|-moz-window-frame-bottom|-moz-window-frame-left|-moz-window-frame-right|-moz-window-titlebar|-moz-window-titlebar-maximized",
        "-moz-binding": "<url>|none",
        "-moz-border-bottom-colors": "<color>+|none",
        "-moz-border-left-colors": "<color>+|none",
        "-moz-border-right-colors": "<color>+|none",
        "-moz-border-top-colors": "<color>+|none",
        "-moz-context-properties": "none|[fill|fill-opacity|stroke|stroke-opacity]#",
        "-moz-float-edge": "border-box|content-box|margin-box|padding-box",
        "-moz-force-broken-image-icon": "0|1",
        "-moz-image-region": "<shape>|auto",
        "-moz-orient": "inline|block|horizontal|vertical",
        "-moz-outline-radius": "<outline-radius>{1,4} [/ <outline-radius>{1,4}]?",
        "-moz-outline-radius-bottomleft": "<outline-radius>",
        "-moz-outline-radius-bottomright": "<outline-radius>",
        "-moz-outline-radius-topleft": "<outline-radius>",
        "-moz-outline-radius-topright": "<outline-radius>",
        "-moz-stack-sizing": "ignore|stretch-to-fit",
        "-moz-text-blink": "none|blink",
        "-moz-user-focus": "ignore|normal|select-after|select-before|select-menu|select-same|select-all|none",
        "-moz-user-input": "auto|none|enabled|disabled",
        "-moz-user-modify": "read-only|read-write|write-only",
        "-moz-window-dragging": "drag|no-drag",
        "-moz-window-shadow": "default|menu|tooltip|sheet|none",
        "-webkit-appearance": "none|button|button-bevel|caps-lock-indicator|caret|checkbox|default-button|inner-spin-button|listbox|listitem|media-controls-background|media-controls-fullscreen-background|media-current-time-display|media-enter-fullscreen-button|media-exit-fullscreen-button|media-fullscreen-button|media-mute-button|media-overlay-play-button|media-play-button|media-seek-back-button|media-seek-forward-button|media-slider|media-sliderthumb|media-time-remaining-display|media-toggle-closed-captions-button|media-volume-slider|media-volume-slider-container|media-volume-sliderthumb|menulist|menulist-button|menulist-text|menulist-textfield|meter|progress-bar|progress-bar-value|push-button|radio|scrollbarbutton-down|scrollbarbutton-left|scrollbarbutton-right|scrollbarbutton-up|scrollbargripper-horizontal|scrollbargripper-vertical|scrollbarthumb-horizontal|scrollbarthumb-vertical|scrollbartrack-horizontal|scrollbartrack-vertical|searchfield|searchfield-cancel-button|searchfield-decoration|searchfield-results-button|searchfield-results-decoration|slider-horizontal|slider-vertical|sliderthumb-horizontal|sliderthumb-vertical|square-button|textarea|textfield|-apple-pay-button",
        "-webkit-border-before": "<'border-width'>||<'border-style'>||<color>",
        "-webkit-border-before-color": "<color>",
        "-webkit-border-before-style": "<'border-style'>",
        "-webkit-border-before-width": "<'border-width'>",
        "-webkit-box-reflect": "[above|below|right|left]? <length>? <image>?",
        "-webkit-line-clamp": "none|<integer>",
        "-webkit-mask": "[<mask-reference>||<position> [/ <bg-size>]?||<repeat-style>||[<box>|border|padding|content|text]||[<box>|border|padding|content]]#",
        "-webkit-mask-attachment": "<attachment>#",
        "-webkit-mask-clip": "[<box>|border|padding|content|text]#",
        "-webkit-mask-composite": "<composite-style>#",
        "-webkit-mask-image": "<mask-reference>#",
        "-webkit-mask-origin": "[<box>|border|padding|content]#",
        "-webkit-mask-position": "<position>#",
        "-webkit-mask-position-x": "[<length-percentage>|left|center|right]#",
        "-webkit-mask-position-y": "[<length-percentage>|top|center|bottom]#",
        "-webkit-mask-repeat": "<repeat-style>#",
        "-webkit-mask-repeat-x": "repeat|no-repeat|space|round",
        "-webkit-mask-repeat-y": "repeat|no-repeat|space|round",
        "-webkit-mask-size": "<bg-size>#",
        "-webkit-overflow-scrolling": "auto|touch",
        "-webkit-tap-highlight-color": "<color>",
        "-webkit-text-fill-color": "<color>",
        "-webkit-text-stroke": "<length>||<color>",
        "-webkit-text-stroke-color": "<color>",
        "-webkit-text-stroke-width": "<length>",
        "-webkit-touch-callout": "default|none",
        "-webkit-user-modify": "read-only|read-write|read-write-plaintext-only",
        "accent-color": "auto|<color>",
        "align-content": "normal|<baseline-position>|<content-distribution>|<overflow-position>? <content-position>",
        "align-items": "normal|stretch|<baseline-position>|[<overflow-position>? <self-position>]",
        "align-self": "auto|normal|stretch|<baseline-position>|<overflow-position>? <self-position>",
        "align-tracks": "[normal|<baseline-position>|<content-distribution>|<overflow-position>? <content-position>]#",
        all: "initial|inherit|unset|revert|revert-layer",
        "anchor-name": "none|<dashed-ident>#",
        "anchor-scope": "none|all|<dashed-ident>#",
        animation: "<single-animation>#",
        "animation-composition": "<single-animation-composition>#",
        "animation-delay": "<time>#",
        "animation-direction": "<single-animation-direction>#",
        "animation-duration": "<time>#",
        "animation-fill-mode": "<single-animation-fill-mode>#",
        "animation-iteration-count": "<single-animation-iteration-count>#",
        "animation-name": "[none|<keyframes-name>]#",
        "animation-play-state": "<single-animation-play-state>#",
        "animation-range": "[<'animation-range-start'> <'animation-range-end'>?]#",
        "animation-range-end": "[normal|<length-percentage>|<timeline-range-name> <length-percentage>?]#",
        "animation-range-start": "[normal|<length-percentage>|<timeline-range-name> <length-percentage>?]#",
        "animation-timing-function": "<easing-function>#",
        "animation-timeline": "<single-animation-timeline>#",
        appearance: "none|auto|textfield|menulist-button|<compat-auto>",
        "aspect-ratio": "auto|<ratio>",
        azimuth: "<angle>|[[left-side|far-left|left|center-left|center|center-right|right|far-right|right-side]||behind]|leftwards|rightwards",
        "backdrop-filter": "none|<filter-function-list>",
        "backface-visibility": "visible|hidden",
        background: "[<bg-layer> ,]* <final-bg-layer>",
        "background-attachment": "<attachment>#",
        "background-blend-mode": "<blend-mode>#",
        "background-clip": "<bg-clip>#",
        "background-color": "<color>",
        "background-image": "<bg-image>#",
        "background-origin": "<box>#",
        "background-position": "<bg-position>#",
        "background-position-x": "[center|[[left|right|x-start|x-end]? <length-percentage>?]!]#",
        "background-position-y": "[center|[[top|bottom|y-start|y-end]? <length-percentage>?]!]#",
        "background-repeat": "<repeat-style>#",
        "background-size": "<bg-size>#",
        "block-size": "<'width'>",
        border: "<line-width>||<line-style>||<color>",
        "border-block": "<'border-top-width'>||<'border-top-style'>||<color>",
        "border-block-color": "<'border-top-color'>{1,2}",
        "border-block-style": "<'border-top-style'>",
        "border-block-width": "<'border-top-width'>",
        "border-block-end": "<'border-top-width'>||<'border-top-style'>||<color>",
        "border-block-end-color": "<'border-top-color'>",
        "border-block-end-style": "<'border-top-style'>",
        "border-block-end-width": "<'border-top-width'>",
        "border-block-start": "<'border-top-width'>||<'border-top-style'>||<color>",
        "border-block-start-color": "<'border-top-color'>",
        "border-block-start-style": "<'border-top-style'>",
        "border-block-start-width": "<'border-top-width'>",
        "border-bottom": "<line-width>||<line-style>||<color>",
        "border-bottom-color": "<'border-top-color'>",
        "border-bottom-left-radius": "<length-percentage>{1,2}",
        "border-bottom-right-radius": "<length-percentage>{1,2}",
        "border-bottom-style": "<line-style>",
        "border-bottom-width": "<line-width>",
        "border-collapse": "collapse|separate",
        "border-color": "<color>{1,4}",
        "border-end-end-radius": "<length-percentage>{1,2}",
        "border-end-start-radius": "<length-percentage>{1,2}",
        "border-image": "<'border-image-source'>||<'border-image-slice'> [/ <'border-image-width'>|/ <'border-image-width'>? / <'border-image-outset'>]?||<'border-image-repeat'>",
        "border-image-outset": "[<length>|<number>]{1,4}",
        "border-image-repeat": "[stretch|repeat|round|space]{1,2}",
        "border-image-slice": "<number-percentage>{1,4}&&fill?",
        "border-image-source": "none|<image>",
        "border-image-width": "[<length-percentage>|<number>|auto]{1,4}",
        "border-inline": "<'border-top-width'>||<'border-top-style'>||<color>",
        "border-inline-end": "<'border-top-width'>||<'border-top-style'>||<color>",
        "border-inline-color": "<'border-top-color'>{1,2}",
        "border-inline-style": "<'border-top-style'>",
        "border-inline-width": "<'border-top-width'>",
        "border-inline-end-color": "<'border-top-color'>",
        "border-inline-end-style": "<'border-top-style'>",
        "border-inline-end-width": "<'border-top-width'>",
        "border-inline-start": "<'border-top-width'>||<'border-top-style'>||<color>",
        "border-inline-start-color": "<'border-top-color'>",
        "border-inline-start-style": "<'border-top-style'>",
        "border-inline-start-width": "<'border-top-width'>",
        "border-left": "<line-width>||<line-style>||<color>",
        "border-left-color": "<color>",
        "border-left-style": "<line-style>",
        "border-left-width": "<line-width>",
        "border-radius": "<length-percentage>{1,4} [/ <length-percentage>{1,4}]?",
        "border-right": "<line-width>||<line-style>||<color>",
        "border-right-color": "<color>",
        "border-right-style": "<line-style>",
        "border-right-width": "<line-width>",
        "border-spacing": "<length> <length>?",
        "border-start-end-radius": "<length-percentage>{1,2}",
        "border-start-start-radius": "<length-percentage>{1,2}",
        "border-style": "<line-style>{1,4}",
        "border-top": "<line-width>||<line-style>||<color>",
        "border-top-color": "<color>",
        "border-top-left-radius": "<length-percentage>{1,2}",
        "border-top-right-radius": "<length-percentage>{1,2}",
        "border-top-style": "<line-style>",
        "border-top-width": "<line-width>",
        "border-width": "<line-width>{1,4}",
        bottom: "<length>|<percentage>|auto",
        "box-align": "start|center|end|baseline|stretch",
        "box-decoration-break": "slice|clone",
        "box-direction": "normal|reverse|inherit",
        "box-flex": "<number>",
        "box-flex-group": "<integer>",
        "box-lines": "single|multiple",
        "box-ordinal-group": "<integer>",
        "box-orient": "horizontal|vertical|inline-axis|block-axis|inherit",
        "box-pack": "start|center|end|justify",
        "box-shadow": "none|<shadow>#",
        "box-sizing": "content-box|border-box",
        "break-after": "auto|avoid|always|all|avoid-page|page|left|right|recto|verso|avoid-column|column|avoid-region|region",
        "break-before": "auto|avoid|always|all|avoid-page|page|left|right|recto|verso|avoid-column|column|avoid-region|region",
        "break-inside": "auto|avoid|avoid-page|avoid-column|avoid-region",
        "caption-side": "top|bottom|block-start|block-end|inline-start|inline-end",
        caret: "<'caret-color'>||<'caret-shape'>",
        "caret-color": "auto|<color>",
        "caret-shape": "auto|bar|block|underscore",
        clear: "none|left|right|both|inline-start|inline-end",
        clip: "<shape>|auto",
        "clip-path": "<clip-source>|[<basic-shape>||<geometry-box>]|none",
        "clip-rule": "nonzero|evenodd",
        color: "<color>",
        "color-interpolation-filters": "auto|sRGB|linearRGB",
        "color-scheme": "normal|[light|dark|<custom-ident>]+&&only?",
        "column-count": "<integer>|auto",
        "column-fill": "auto|balance|balance-all",
        "column-gap": "normal|<length-percentage>",
        "column-rule": "<'column-rule-width'>||<'column-rule-style'>||<'column-rule-color'>",
        "column-rule-color": "<color>",
        "column-rule-style": "<'border-style'>",
        "column-rule-width": "<'border-width'>",
        "column-span": "none|all",
        "column-width": "<length>|auto",
        columns: "<'column-width'>||<'column-count'>",
        contain: "none|strict|content|[[size||inline-size]||layout||style||paint]",
        "contain-intrinsic-size": "[auto? [none|<length>]]{1,2}",
        "contain-intrinsic-block-size": "auto? [none|<length>]",
        "contain-intrinsic-height": "auto? [none|<length>]",
        "contain-intrinsic-inline-size": "auto? [none|<length>]",
        "contain-intrinsic-width": "auto? [none|<length>]",
        container: "<'container-name'> [/ <'container-type'>]?",
        "container-name": "none|<custom-ident>+",
        "container-type": "normal||[size|inline-size]",
        content: "normal|none|[<content-replacement>|<content-list>] [/ [<string>|<counter>]+]?",
        "content-visibility": "visible|auto|hidden",
        "counter-increment": "[<counter-name> <integer>?]+|none",
        "counter-reset": "[<counter-name> <integer>?|<reversed-counter-name> <integer>?]+|none",
        "counter-set": "[<counter-name> <integer>?]+|none",
        cursor: "[[<url> [<x> <y>]? ,]* [auto|default|none|context-menu|help|pointer|progress|wait|cell|crosshair|text|vertical-text|alias|copy|move|no-drop|not-allowed|e-resize|n-resize|ne-resize|nw-resize|s-resize|se-resize|sw-resize|w-resize|ew-resize|ns-resize|nesw-resize|nwse-resize|col-resize|row-resize|all-scroll|zoom-in|zoom-out|grab|grabbing|hand|-webkit-grab|-webkit-grabbing|-webkit-zoom-in|-webkit-zoom-out|-moz-grab|-moz-grabbing|-moz-zoom-in|-moz-zoom-out]]",
        d: "none|path( <string> )",
        cx: "<length>|<percentage>",
        cy: "<length>|<percentage>",
        direction: "ltr|rtl",
        display: "[<display-outside>||<display-inside>]|<display-listitem>|<display-internal>|<display-box>|<display-legacy>|<-non-standard-display>",
        "dominant-baseline": "auto|use-script|no-change|reset-size|ideographic|alphabetic|hanging|mathematical|central|middle|text-after-edge|text-before-edge",
        "empty-cells": "show|hide",
        "field-sizing": "content|fixed",
        fill: "<paint>",
        "fill-opacity": "<number-zero-one>",
        "fill-rule": "nonzero|evenodd",
        filter: "none|<filter-function-list>|<-ms-filter-function-list>",
        flex: "none|[<'flex-grow'> <'flex-shrink'>?||<'flex-basis'>]",
        "flex-basis": "content|<'width'>",
        "flex-direction": "row|row-reverse|column|column-reverse",
        "flex-flow": "<'flex-direction'>||<'flex-wrap'>",
        "flex-grow": "<number>",
        "flex-shrink": "<number>",
        "flex-wrap": "nowrap|wrap|wrap-reverse",
        float: "left|right|none|inline-start|inline-end",
        font: "[[<'font-style'>||<font-variant-css2>||<'font-weight'>||<font-width-css3>]? <'font-size'> [/ <'line-height'>]? <'font-family'>#]|<system-family-name>|<-non-standard-font>",
        "font-family": "[<family-name>|<generic-family>]#",
        "font-feature-settings": "normal|<feature-tag-value>#",
        "font-kerning": "auto|normal|none",
        "font-language-override": "normal|<string>",
        "font-optical-sizing": "auto|none",
        "font-palette": "normal|light|dark|<palette-identifier>",
        "font-variation-settings": "normal|[<string> <number>]#",
        "font-size": "<absolute-size>|<relative-size>|<length-percentage>",
        "font-size-adjust": "none|[ex-height|cap-height|ch-width|ic-width|ic-height]? [from-font|<number>]",
        "font-smooth": "auto|never|always|<absolute-size>|<length>",
        "font-stretch": "<font-stretch-absolute>",
        "font-style": "normal|italic|oblique <angle>?",
        "font-synthesis": "none|[weight||style||small-caps||position]",
        "font-synthesis-position": "auto|none",
        "font-synthesis-small-caps": "auto|none",
        "font-synthesis-style": "auto|none",
        "font-synthesis-weight": "auto|none",
        "font-variant": "normal|none|[<common-lig-values>||<discretionary-lig-values>||<historical-lig-values>||<contextual-alt-values>||stylistic( <feature-value-name> )||historical-forms||styleset( <feature-value-name># )||character-variant( <feature-value-name># )||swash( <feature-value-name> )||ornaments( <feature-value-name> )||annotation( <feature-value-name> )||[small-caps|all-small-caps|petite-caps|all-petite-caps|unicase|titling-caps]||<numeric-figure-values>||<numeric-spacing-values>||<numeric-fraction-values>||ordinal||slashed-zero||<east-asian-variant-values>||<east-asian-width-values>||ruby]",
        "font-variant-alternates": "normal|[stylistic( <feature-value-name> )||historical-forms||styleset( <feature-value-name># )||character-variant( <feature-value-name># )||swash( <feature-value-name> )||ornaments( <feature-value-name> )||annotation( <feature-value-name> )]",
        "font-variant-caps": "normal|small-caps|all-small-caps|petite-caps|all-petite-caps|unicase|titling-caps",
        "font-variant-east-asian": "normal|[<east-asian-variant-values>||<east-asian-width-values>||ruby]",
        "font-variant-emoji": "normal|text|emoji|unicode",
        "font-variant-ligatures": "normal|none|[<common-lig-values>||<discretionary-lig-values>||<historical-lig-values>||<contextual-alt-values>]",
        "font-variant-numeric": "normal|[<numeric-figure-values>||<numeric-spacing-values>||<numeric-fraction-values>||ordinal||slashed-zero]",
        "font-variant-position": "normal|sub|super",
        "font-weight": "<font-weight-absolute>|bolder|lighter",
        "forced-color-adjust": "auto|none",
        gap: "<'row-gap'> <'column-gap'>?",
        grid: "<'grid-template'>|<'grid-template-rows'> / [auto-flow&&dense?] <'grid-auto-columns'>?|[auto-flow&&dense?] <'grid-auto-rows'>? / <'grid-template-columns'>",
        "grid-area": "<grid-line> [/ <grid-line>]{0,3}",
        "grid-auto-columns": "<track-size>+",
        "grid-auto-flow": "[row|column]||dense",
        "grid-auto-rows": "<track-size>+",
        "grid-column": "<grid-line> [/ <grid-line>]?",
        "grid-column-end": "<grid-line>",
        "grid-column-gap": "<length-percentage>",
        "grid-column-start": "<grid-line>",
        "grid-gap": "<'grid-row-gap'> <'grid-column-gap'>?",
        "grid-row": "<grid-line> [/ <grid-line>]?",
        "grid-row-end": "<grid-line>",
        "grid-row-gap": "<length-percentage>",
        "grid-row-start": "<grid-line>",
        "grid-template": "none|[<'grid-template-rows'> / <'grid-template-columns'>]|[<line-names>? <string> <track-size>? <line-names>?]+ [/ <explicit-track-list>]?",
        "grid-template-areas": "none|<string>+",
        "grid-template-columns": "none|<track-list>|<auto-track-list>|subgrid <line-name-list>?",
        "grid-template-rows": "none|<track-list>|<auto-track-list>|subgrid <line-name-list>?",
        "hanging-punctuation": "none|[first||[force-end|allow-end]||last]",
        height: "auto|<length>|<percentage>|min-content|max-content|fit-content|fit-content( <length-percentage> )|stretch|<-non-standard-size>",
        "hyphenate-character": "auto|<string>",
        "hyphenate-limit-chars": "[auto|<integer>]{1,3}",
        hyphens: "none|manual|auto",
        "image-orientation": "from-image|<angle>|[<angle>? flip]",
        "image-rendering": "auto|crisp-edges|pixelated|optimizeSpeed|optimizeQuality|<-non-standard-image-rendering>",
        "image-resolution": "[from-image||<resolution>]&&snap?",
        "ime-mode": "auto|normal|active|inactive|disabled",
        "initial-letter": "normal|[<number> <integer>?]",
        "initial-letter-align": "[auto|alphabetic|hanging|ideographic]",
        "inline-size": "<'width'>",
        "input-security": "auto|none",
        inset: "<'top'>{1,4}",
        "inset-area": "none|<inset-area>",
        "inset-block": "<'top'>{1,2}",
        "inset-block-end": "<'top'>",
        "inset-block-start": "<'top'>",
        "inset-inline": "<'top'>{1,2}",
        "inset-inline-end": "<'top'>",
        "inset-inline-start": "<'top'>",
        isolation: "auto|isolate",
        "justify-content": "normal|<content-distribution>|<overflow-position>? [<content-position>|left|right]",
        "justify-items": "normal|stretch|<baseline-position>|<overflow-position>? [<self-position>|left|right]|legacy|legacy&&[left|right|center]",
        "justify-self": "auto|normal|stretch|<baseline-position>|<overflow-position>? [<self-position>|left|right]",
        "justify-tracks": "[normal|<content-distribution>|<overflow-position>? [<content-position>|left|right]]#",
        left: "<length>|<percentage>|auto",
        "letter-spacing": "normal|<length-percentage>",
        "line-break": "auto|loose|normal|strict|anywhere",
        "line-clamp": "none|<integer>",
        "line-height": "normal|<number>|<length>|<percentage>",
        "line-height-step": "<length>",
        "list-style": "<'list-style-type'>||<'list-style-position'>||<'list-style-image'>",
        "list-style-image": "<image>|none",
        "list-style-position": "inside|outside",
        "list-style-type": "<counter-style>|<string>|none",
        margin: "[<length>|<percentage>|auto]{1,4}",
        "margin-block": "<'margin-left'>{1,2}",
        "margin-block-end": "<'margin-left'>",
        "margin-block-start": "<'margin-left'>",
        "margin-bottom": "<length>|<percentage>|auto",
        "margin-inline": "<'margin-left'>{1,2}",
        "margin-inline-end": "<'margin-left'>",
        "margin-inline-start": "<'margin-left'>",
        "margin-left": "<length>|<percentage>|auto",
        "margin-right": "<length>|<percentage>|auto",
        "margin-top": "<length>|<percentage>|auto",
        "margin-trim": "none|in-flow|all",
        marker: "none|<url>",
        "marker-end": "none|<url>",
        "marker-mid": "none|<url>",
        "marker-start": "none|<url>",
        mask: "<mask-layer>#",
        "mask-border": "<'mask-border-source'>||<'mask-border-slice'> [/ <'mask-border-width'>? [/ <'mask-border-outset'>]?]?||<'mask-border-repeat'>||<'mask-border-mode'>",
        "mask-border-mode": "luminance|alpha",
        "mask-border-outset": "[<length>|<number>]{1,4}",
        "mask-border-repeat": "[stretch|repeat|round|space]{1,2}",
        "mask-border-slice": "<number-percentage>{1,4} fill?",
        "mask-border-source": "none|<image>",
        "mask-border-width": "[<length-percentage>|<number>|auto]{1,4}",
        "mask-clip": "[<geometry-box>|no-clip]#",
        "mask-composite": "<compositing-operator>#",
        "mask-image": "<mask-reference>#",
        "mask-mode": "<masking-mode>#",
        "mask-origin": "<geometry-box>#",
        "mask-position": "<position>#",
        "mask-repeat": "<repeat-style>#",
        "mask-size": "<bg-size>#",
        "mask-type": "luminance|alpha",
        "masonry-auto-flow": "[pack|next]||[definite-first|ordered]",
        "math-depth": "auto-add|add( <integer> )|<integer>",
        "math-shift": "normal|compact",
        "math-style": "normal|compact",
        "max-block-size": "<'max-width'>",
        "max-height": "none|<length-percentage>|min-content|max-content|fit-content|fit-content( <length-percentage> )|stretch|<-non-standard-size>",
        "max-inline-size": "<'max-width'>",
        "max-lines": "none|<integer>",
        "max-width": "none|<length-percentage>|min-content|max-content|fit-content|fit-content( <length-percentage> )|stretch|<-non-standard-size>",
        "min-block-size": "<'min-width'>",
        "min-height": "auto|<length>|<percentage>|min-content|max-content|fit-content|fit-content( <length-percentage> )|stretch|<-non-standard-size>",
        "min-inline-size": "<'min-width'>",
        "min-width": "auto|<length>|<percentage>|min-content|max-content|fit-content|fit-content( <length-percentage> )|stretch|<-non-standard-size>",
        "mix-blend-mode": "<blend-mode>|plus-lighter",
        "object-fit": "fill|contain|cover|none|scale-down",
        "object-position": "<position>",
        offset: "[<'offset-position'>? [<'offset-path'> [<'offset-distance'>||<'offset-rotate'>]?]?]! [/ <'offset-anchor'>]?",
        "offset-anchor": "auto|<position>",
        "offset-distance": "<length-percentage>",
        "offset-path": "none|<offset-path>||<coord-box>",
        "offset-position": "normal|auto|<position>",
        "offset-rotate": "[auto|reverse]||<angle>",
        opacity: "<alpha-value>",
        order: "<integer>",
        orphans: "<integer>",
        outline: "[<'outline-width'>||<'outline-style'>||<'outline-color'>]",
        "outline-color": "auto|<color>",
        "outline-offset": "<length>",
        "outline-style": "auto|<'border-style'>",
        "outline-width": "<line-width>",
        overflow: "[visible|hidden|clip|scroll|auto]{1,2}|<-non-standard-overflow>",
        "overflow-anchor": "auto|none",
        "overflow-block": "visible|hidden|clip|scroll|auto",
        "overflow-clip-box": "padding-box|content-box",
        "overflow-clip-margin": "<visual-box>||<length [0,∞]>",
        "overflow-inline": "visible|hidden|clip|scroll|auto",
        "overflow-wrap": "normal|break-word|anywhere",
        "overflow-x": "visible|hidden|clip|scroll|auto",
        "overflow-y": "visible|hidden|clip|scroll|auto",
        overlay: "none|auto",
        "overscroll-behavior": "[contain|none|auto]{1,2}",
        "overscroll-behavior-block": "contain|none|auto",
        "overscroll-behavior-inline": "contain|none|auto",
        "overscroll-behavior-x": "contain|none|auto",
        "overscroll-behavior-y": "contain|none|auto",
        padding: "[<length>|<percentage>]{1,4}",
        "padding-block": "<'padding-left'>{1,2}",
        "padding-block-end": "<'padding-left'>",
        "padding-block-start": "<'padding-left'>",
        "padding-bottom": "<length>|<percentage>",
        "padding-inline": "<'padding-left'>{1,2}",
        "padding-inline-end": "<'padding-left'>",
        "padding-inline-start": "<'padding-left'>",
        "padding-left": "<length>|<percentage>",
        "padding-right": "<length>|<percentage>",
        "padding-top": "<length>|<percentage>",
        page: "auto|<custom-ident>",
        "page-break-after": "auto|always|avoid|left|right|recto|verso",
        "page-break-before": "auto|always|avoid|left|right|recto|verso",
        "page-break-inside": "auto|avoid",
        "paint-order": "normal|[fill||stroke||markers]",
        perspective: "none|<length>",
        "perspective-origin": "<position>",
        "place-content": "<'align-content'> <'justify-content'>?",
        "place-items": "<'align-items'> <'justify-items'>?",
        "place-self": "<'align-self'> <'justify-self'>?",
        "pointer-events": "auto|none|visiblePainted|visibleFill|visibleStroke|visible|painted|fill|stroke|all|inherit",
        position: "static|relative|absolute|sticky|fixed|-webkit-sticky",
        "position-anchor": "<anchor-element>",
        "position-try": "<'position-try-order'>? <'position-try-fallbacks'>",
        "position-try-fallbacks": "none|[[<dashed-ident>||<try-tactic>]|<'position-area'>]#",
        "position-try-order": "normal|<try-size>",
        "position-visibility": "always|[anchors-valid||anchors-visible||no-overflow]",
        "print-color-adjust": "economy|exact",
        quotes: "none|auto|[<string> <string>]+",
        r: "<length>|<percentage>",
        resize: "none|both|horizontal|vertical|block|inline",
        right: "<length>|<percentage>|auto",
        rotate: "none|<angle>|[x|y|z|<number>{3}]&&<angle>",
        "row-gap": "normal|<length-percentage>",
        "ruby-align": "start|center|space-between|space-around",
        "ruby-merge": "separate|collapse|auto",
        "ruby-position": "[alternate||[over|under]]|inter-character",
        rx: "<length>|<percentage>",
        ry: "<length>|<percentage>",
        scale: "none|<number>{1,3}",
        "scrollbar-color": "auto|<color>{2}",
        "scrollbar-gutter": "auto|stable&&both-edges?",
        "scrollbar-width": "auto|thin|none",
        "scroll-behavior": "auto|smooth",
        "scroll-margin": "<length>{1,4}",
        "scroll-margin-block": "<length>{1,2}",
        "scroll-margin-block-start": "<length>",
        "scroll-margin-block-end": "<length>",
        "scroll-margin-bottom": "<length>",
        "scroll-margin-inline": "<length>{1,2}",
        "scroll-margin-inline-start": "<length>",
        "scroll-margin-inline-end": "<length>",
        "scroll-margin-left": "<length>",
        "scroll-margin-right": "<length>",
        "scroll-margin-top": "<length>",
        "scroll-padding": "[auto|<length-percentage>]{1,4}",
        "scroll-padding-block": "[auto|<length-percentage>]{1,2}",
        "scroll-padding-block-start": "auto|<length-percentage>",
        "scroll-padding-block-end": "auto|<length-percentage>",
        "scroll-padding-bottom": "auto|<length-percentage>",
        "scroll-padding-inline": "[auto|<length-percentage>]{1,2}",
        "scroll-padding-inline-start": "auto|<length-percentage>",
        "scroll-padding-inline-end": "auto|<length-percentage>",
        "scroll-padding-left": "auto|<length-percentage>",
        "scroll-padding-right": "auto|<length-percentage>",
        "scroll-padding-top": "auto|<length-percentage>",
        "scroll-snap-align": "[none|start|end|center]{1,2}",
        "scroll-snap-coordinate": "none|<position>#",
        "scroll-snap-destination": "<position>",
        "scroll-snap-points-x": "none|repeat( <length-percentage> )",
        "scroll-snap-points-y": "none|repeat( <length-percentage> )",
        "scroll-snap-stop": "normal|always",
        "scroll-snap-type": "none|[x|y|block|inline|both] [mandatory|proximity]?",
        "scroll-snap-type-x": "none|mandatory|proximity",
        "scroll-snap-type-y": "none|mandatory|proximity",
        "scroll-timeline": "[<'scroll-timeline-name'>||<'scroll-timeline-axis'>]#",
        "scroll-timeline-axis": "[block|inline|x|y]#",
        "scroll-timeline-name": "[none|<dashed-ident>]#",
        "shape-image-threshold": "<alpha-value>",
        "shape-margin": "<length-percentage>",
        "shape-outside": "none|[<shape-box>||<basic-shape>]|<image>",
        "shape-rendering": "auto|optimizeSpeed|crispEdges|geometricPrecision",
        "tab-size": "<integer>|<length>",
        "table-layout": "auto|fixed",
        "text-align": "start|end|left|right|center|justify|match-parent",
        "text-align-last": "auto|start|end|left|right|center|justify",
        "text-anchor": "start|middle|end",
        "text-combine-upright": "none|all|[digits <integer>?]",
        "text-decoration": "<'text-decoration-line'>||<'text-decoration-style'>||<'text-decoration-color'>||<'text-decoration-thickness'>",
        "text-decoration-color": "<color>",
        "text-decoration-line": "none|[underline||overline||line-through||blink]|spelling-error|grammar-error",
        "text-decoration-skip": "none|[objects||[spaces|[leading-spaces||trailing-spaces]]||edges||box-decoration]",
        "text-decoration-skip-ink": "auto|all|none",
        "text-decoration-style": "solid|double|dotted|dashed|wavy",
        "text-decoration-thickness": "auto|from-font|<length>|<percentage>",
        "text-emphasis": "<'text-emphasis-style'>||<'text-emphasis-color'>",
        "text-emphasis-color": "<color>",
        "text-emphasis-position": "[over|under]&&[right|left]",
        "text-emphasis-style": "none|[[filled|open]||[dot|circle|double-circle|triangle|sesame]]|<string>",
        "text-indent": "<length-percentage>&&hanging?&&each-line?",
        "text-justify": "auto|inter-character|inter-word|none",
        "text-orientation": "mixed|upright|sideways",
        "text-overflow": "[clip|ellipsis|<string>]{1,2}",
        "text-rendering": "auto|optimizeSpeed|optimizeLegibility|geometricPrecision",
        "text-shadow": "none|<shadow-t>#",
        "text-size-adjust": "none|auto|<percentage>",
        "text-spacing-trim": "space-all|normal|space-first|trim-start|trim-both|trim-all|auto",
        "text-transform": "none|capitalize|uppercase|lowercase|full-width|full-size-kana",
        "text-underline-offset": "auto|<length>|<percentage>",
        "text-underline-position": "auto|from-font|[under||[left|right]]",
        "text-wrap": "wrap|nowrap|balance|stable|pretty",
        "text-wrap-mode": "auto|wrap|nowrap",
        "text-wrap-style": "auto|balance|stable|pretty",
        "timeline-scope": "none|<dashed-ident>#",
        top: "<length>|<percentage>|auto",
        "touch-action": "auto|none|[[pan-x|pan-left|pan-right]||[pan-y|pan-up|pan-down]||pinch-zoom]|manipulation",
        transform: "none|<transform-list>",
        "transform-box": "content-box|border-box|fill-box|stroke-box|view-box",
        "transform-origin": "[<length-percentage>|left|center|right|top|bottom]|[[<length-percentage>|left|center|right]&&[<length-percentage>|top|center|bottom]] <length>?",
        "transform-style": "flat|preserve-3d",
        transition: "<single-transition>#",
        "transition-behavior": "<transition-behavior-value>#",
        "transition-delay": "<time>#",
        "transition-duration": "<time>#",
        "transition-property": "none|<single-transition-property>#",
        "transition-timing-function": "<easing-function>#",
        translate: "none|<length-percentage> [<length-percentage> <length>?]?",
        "unicode-bidi": "normal|embed|isolate|bidi-override|isolate-override|plaintext|-moz-isolate|-moz-isolate-override|-moz-plaintext|-webkit-isolate|-webkit-isolate-override|-webkit-plaintext",
        "user-select": "auto|text|none|contain|all",
        "vector-effect": "none|non-scaling-stroke|non-scaling-size|non-rotation|fixed-position",
        "vertical-align": "baseline|sub|super|text-top|text-bottom|middle|top|bottom|<percentage>|<length>",
        "view-timeline": "[<'view-timeline-name'> <'view-timeline-axis'>?]#",
        "view-timeline-axis": "[block|inline|x|y]#",
        "view-timeline-inset": "[[auto|<length-percentage>]{1,2}]#",
        "view-timeline-name": "none|<dashed-ident>#",
        "view-transition-name": "none|<custom-ident>",
        visibility: "visible|hidden|collapse",
        "white-space": "normal|pre|nowrap|pre-wrap|pre-line|break-spaces|[<'white-space-collapse'>||<'text-wrap'>||<'white-space-trim'>]",
        "white-space-collapse": "collapse|discard|preserve|preserve-breaks|preserve-spaces|break-spaces",
        widows: "<integer>",
        width: "auto|<length>|<percentage>|min-content|max-content|fit-content|fit-content( <length-percentage> )|stretch|<-non-standard-size>",
        "will-change": "auto|<animateable-feature>#",
        "word-break": "normal|break-all|keep-all|break-word|auto-phrase",
        "word-spacing": "normal|<length>",
        "word-wrap": "normal|break-word",
        "writing-mode": "horizontal-tb|vertical-rl|vertical-lr|sideways-rl|sideways-lr|<svg-writing-mode>",
        x: "<length>|<percentage>",
        y: "<length>|<percentage>",
        "z-index": "auto|<integer>",
        zoom: "normal|reset|<number>|<percentage>",
        "-moz-background-clip": "padding|border",
        "-moz-border-radius-bottomleft": "<'border-bottom-left-radius'>",
        "-moz-border-radius-bottomright": "<'border-bottom-right-radius'>",
        "-moz-border-radius-topleft": "<'border-top-left-radius'>",
        "-moz-border-radius-topright": "<'border-bottom-right-radius'>",
        "-moz-control-character-visibility": "visible|hidden",
        "-moz-osx-font-smoothing": "auto|grayscale",
        "-moz-user-select": "none|text|all|-moz-none",
        "-ms-flex-align": "start|end|center|baseline|stretch",
        "-ms-flex-item-align": "auto|start|end|center|baseline|stretch",
        "-ms-flex-line-pack": "start|end|center|justify|distribute|stretch",
        "-ms-flex-negative": "<'flex-shrink'>",
        "-ms-flex-pack": "start|end|center|justify|distribute",
        "-ms-flex-order": "<integer>",
        "-ms-flex-positive": "<'flex-grow'>",
        "-ms-flex-preferred-size": "<'flex-basis'>",
        "-ms-interpolation-mode": "nearest-neighbor|bicubic",
        "-ms-grid-column-align": "start|end|center|stretch",
        "-ms-grid-row-align": "start|end|center|stretch",
        "-ms-hyphenate-limit-last": "none|always|column|page|spread",
        "-webkit-background-clip": "[<box>|border|padding|content|text]#",
        "-webkit-column-break-after": "always|auto|avoid",
        "-webkit-column-break-before": "always|auto|avoid",
        "-webkit-column-break-inside": "always|auto|avoid",
        "-webkit-font-smoothing": "auto|none|antialiased|subpixel-antialiased",
        "-webkit-mask-box-image": "[<url>|<gradient>|none] [<length-percentage>{4} <-webkit-mask-box-repeat>{2}]?",
        "-webkit-print-color-adjust": "economy|exact",
        "-webkit-text-security": "none|circle|disc|square",
        "-webkit-user-drag": "none|element|auto",
        "-webkit-user-select": "auto|none|text|all",
        "alignment-baseline": "auto|baseline|before-edge|text-before-edge|middle|central|after-edge|text-after-edge|ideographic|alphabetic|hanging|mathematical",
        "baseline-shift": "baseline|sub|super|<svg-length>",
        behavior: "<url>+",
        cue: "<'cue-before'> <'cue-after'>?",
        "cue-after": "<url> <decibel>?|none",
        "cue-before": "<url> <decibel>?|none",
        "glyph-orientation-horizontal": "<angle>",
        "glyph-orientation-vertical": "<angle>",
        kerning: "auto|<svg-length>",
        pause: "<'pause-before'> <'pause-after'>?",
        "pause-after": "<time>|none|x-weak|weak|medium|strong|x-strong",
        "pause-before": "<time>|none|x-weak|weak|medium|strong|x-strong",
        rest: "<'rest-before'> <'rest-after'>?",
        "rest-after": "<time>|none|x-weak|weak|medium|strong|x-strong",
        "rest-before": "<time>|none|x-weak|weak|medium|strong|x-strong",
        src: "[<url> [format( <string># )]?|local( <family-name> )]#",
        speak: "auto|never|always",
        "speak-as": "normal|spell-out||digits||[literal-punctuation|no-punctuation]",
        stroke: "<paint>",
        "stroke-dasharray": "none|[<svg-length>+]#",
        "stroke-dashoffset": "<svg-length>",
        "stroke-linecap": "butt|round|square",
        "stroke-linejoin": "miter|round|bevel",
        "stroke-miterlimit": "<number-one-or-greater>",
        "stroke-opacity": "<number-zero-one>",
        "stroke-width": "<svg-length>",
        "unicode-range": "<urange>#",
        "voice-balance": "<number>|left|center|right|leftwards|rightwards",
        "voice-duration": "auto|<time>",
        "voice-family": "[[<family-name>|<generic-voice>] ,]* [<family-name>|<generic-voice>]|preserve",
        "voice-pitch": "<frequency>&&absolute|[[x-low|low|medium|high|x-high]||[<frequency>|<semitones>|<percentage>]]",
        "voice-range": "<frequency>&&absolute|[[x-low|low|medium|high|x-high]||[<frequency>|<semitones>|<percentage>]]",
        "voice-rate": "[normal|x-slow|slow|medium|fast|x-fast]||<percentage>",
        "voice-stress": "normal|strong|moderate|none|reduced",
        "voice-volume": "silent|[[x-soft|soft|medium|loud|x-loud]||<decibel>]",
        "white-space-trim": "none|discard-before||discard-after||discard-inner",
        "position-area": "none|<position-area>"
      },
      atrules: {
        charset: {
          prelude: "<string>",
          descriptors: null
        },
        "counter-style": {
          prelude: "<counter-style-name>",
          descriptors: {
            "additive-symbols": "[<integer>&&<symbol>]#",
            fallback: "<counter-style-name>",
            negative: "<symbol> <symbol>?",
            pad: "<integer>&&<symbol>",
            prefix: "<symbol>",
            range: "[[<integer>|infinite]{2}]#|auto",
            "speak-as": "auto|bullets|numbers|words|spell-out|<counter-style-name>",
            suffix: "<symbol>",
            symbols: "<symbol>+",
            system: "cyclic|numeric|alphabetic|symbolic|additive|[fixed <integer>?]|[extends <counter-style-name>]"
          }
        },
        document: {
          prelude: "[<url>|url-prefix( <string> )|domain( <string> )|media-document( <string> )|regexp( <string> )]#",
          descriptors: null
        },
        "font-palette-values": {
          prelude: "<dashed-ident>",
          descriptors: {
            "base-palette": "light|dark|<integer [0,∞]>",
            "font-family": "<family-name>#",
            "override-colors": "[<integer [0,∞]> <absolute-color-base>]#"
          }
        },
        "font-face": {
          prelude: null,
          descriptors: {
            "ascent-override": "normal|<percentage>",
            "descent-override": "normal|<percentage>",
            "font-display": "[auto|block|swap|fallback|optional]",
            "font-family": "<family-name>",
            "font-feature-settings": "normal|<feature-tag-value>#",
            "font-variation-settings": "normal|[<string> <number>]#",
            "font-stretch": "<font-stretch-absolute>{1,2}",
            "font-style": "normal|italic|oblique <angle>{0,2}",
            "font-weight": "<font-weight-absolute>{1,2}",
            "line-gap-override": "normal|<percentage>",
            "size-adjust": "<percentage>",
            src: "[<url> [format( <string># )]?|local( <family-name> )]#",
            "unicode-range": "<urange>#"
          }
        },
        "font-feature-values": {
          prelude: "<family-name>#",
          descriptors: null
        },
        import: {
          prelude: "[<string>|<url>] [layer|layer( <layer-name> )]? [supports( [<supports-condition>|<declaration>] )]? <media-query-list>?",
          descriptors: null
        },
        keyframes: {
          prelude: "<keyframes-name>",
          descriptors: null
        },
        layer: {
          prelude: "[<layer-name>#|<layer-name>?]",
          descriptors: null
        },
        media: {
          prelude: "<media-query-list>",
          descriptors: null
        },
        namespace: {
          prelude: "<namespace-prefix>? [<string>|<url>]",
          descriptors: null
        },
        page: {
          prelude: "<page-selector-list>",
          descriptors: {
            bleed: "auto|<length>",
            marks: "none|[crop||cross]",
            "page-orientation": "upright|rotate-left|rotate-right",
            size: "<length>{1,2}|auto|[<page-size>||[portrait|landscape]]"
          }
        },
        "position-try": {
          prelude: "<dashed-ident>",
          descriptors: {
            top: "<'top'>",
            left: "<'left'>",
            bottom: "<'bottom'>",
            right: "<'right'>",
            "inset-block-start": "<'inset-block-start'>",
            "inset-block-end": "<'inset-block-end'>",
            "inset-inline-start": "<'inset-inline-start'>",
            "inset-inline-end": "<'inset-inline-end'>",
            "inset-block": "<'inset-block'>",
            "inset-inline": "<'inset-inline'>",
            inset: "<'inset'>",
            "margin-top": "<'margin-top'>",
            "margin-left": "<'margin-left'>",
            "margin-bottom": "<'margin-bottom'>",
            "margin-right": "<'margin-right'>",
            "margin-block-start": "<'margin-block-start'>",
            "margin-block-end": "<'margin-block-end'>",
            "margin-inline-start": "<'margin-inline-start'>",
            "margin-inline-end": "<'margin-inline-end'>",
            margin: "<'margin'>",
            "margin-block": "<'margin-block'>",
            "margin-inline": "<'margin-inline'>",
            width: "<'width'>",
            height: "<'height'>",
            "min-width": "<'min-width'>",
            "min-height": "<'min-height'>",
            "max-width": "<'max-width'>",
            "max-height": "<'max-height'>",
            "block-size": "<'block-size'>",
            "inline-size": "<'inline-size'>",
            "min-block-size": "<'min-block-size'>",
            "min-inline-size": "<'min-inline-size'>",
            "max-block-size": "<'max-block-size'>",
            "max-inline-size": "<'max-inline-size'>",
            "align-self": "<'align-self'>|anchor-center",
            "justify-self": "<'justify-self'>|anchor-center"
          }
        },
        property: {
          prelude: "<custom-property-name>",
          descriptors: {
            syntax: "<string>",
            inherits: "true|false",
            "initial-value": "<declaration-value>?"
          }
        },
        scope: {
          prelude: "[( <scope-start> )]? [to ( <scope-end> )]?",
          descriptors: null
        },
        "starting-style": {
          prelude: null,
          descriptors: null
        },
        supports: {
          prelude: "<supports-condition>",
          descriptors: null
        },
        container: {
          prelude: "[<container-name>]? <container-condition>",
          descriptors: null
        },
        nest: {
          prelude: "<complex-selector-list>",
          descriptors: null
        }
      }
    }, Ee = 43, he = 45, Kt = 110, Ge = !0, Zc = !1;
    function Yt(e, t) {
      let n = this.tokenStart + e;
      const r = this.charCodeAt(n);
      for ((r === Ee || r === he) && (t && this.error("Number sign is not allowed"), n++); n < this.tokenEnd; n++)
        Q(this.charCodeAt(n)) || this.error("Integer is expected", n);
    }
    function st(e) {
      return Yt.call(this, 0, e);
    }
    function Me(e, t) {
      if (!this.cmpChar(this.tokenStart + e, t)) {
        let n = "";
        switch (t) {
          case Kt:
            n = "N is expected";
            break;
          case he:
            n = "HyphenMinus is expected";
            break;
        }
        this.error(n, this.tokenStart + e);
      }
    }
    function jn() {
      let e = 0, t = 0, n = this.tokenType;
      for (; n === W || n === X; )
        n = this.lookupType(++e);
      if (n !== L)
        if (this.isDelim(Ee, e) || this.isDelim(he, e)) {
          t = this.isDelim(Ee, e) ? Ee : he;
          do
            n = this.lookupType(++e);
          while (n === W || n === X);
          n !== L && (this.skip(e), st.call(this, Ge));
        } else
          return null;
      return e > 0 && this.skip(e), t === 0 && (n = this.charCodeAt(this.tokenStart), n !== Ee && n !== he && this.error("Number sign is expected")), st.call(this, t !== 0), t === he ? "-" + this.consume(L) : this.consume(L);
    }
    const Jc = "AnPlusB", eu = {
      a: [String, null],
      b: [String, null]
    };
    function Ko() {
      const e = this.tokenStart;
      let t = null, n = null;
      if (this.tokenType === L)
        st.call(this, Zc), n = this.consume(L);
      else if (this.tokenType === y && this.cmpChar(this.tokenStart, he))
        switch (t = "-1", Me.call(this, 1, Kt), this.tokenEnd - this.tokenStart) {
          case 2:
            this.next(), n = jn.call(this);
            break;
          case 3:
            Me.call(this, 2, he), this.next(), this.skipSC(), st.call(this, Ge), n = "-" + this.consume(L);
            break;
          default:
            Me.call(this, 2, he), Yt.call(this, 3, Ge), this.next(), n = this.substrToCursor(e + 2);
        }
      else if (this.tokenType === y || this.isDelim(Ee) && this.lookupType(1) === y) {
        let r = 0;
        switch (t = "1", this.isDelim(Ee) && (r = 1, this.next()), Me.call(this, 0, Kt), this.tokenEnd - this.tokenStart) {
          case 1:
            this.next(), n = jn.call(this);
            break;
          case 2:
            Me.call(this, 1, he), this.next(), this.skipSC(), st.call(this, Ge), n = "-" + this.consume(L);
            break;
          default:
            Me.call(this, 1, he), Yt.call(this, 2, Ge), this.next(), n = this.substrToCursor(e + r + 1);
        }
      } else if (this.tokenType === z) {
        const r = this.charCodeAt(this.tokenStart), i = r === Ee || r === he;
        let o = this.tokenStart + i;
        for (; o < this.tokenEnd && Q(this.charCodeAt(o)); o++)
          ;
        o === this.tokenStart + i && this.error("Integer is expected", this.tokenStart + i), Me.call(this, o - this.tokenStart, Kt), t = this.substring(e, o), o + 1 === this.tokenEnd ? (this.next(), n = jn.call(this)) : (Me.call(this, o - this.tokenStart + 1, he), o + 2 === this.tokenEnd ? (this.next(), this.skipSC(), st.call(this, Ge), n = "-" + this.consume(L)) : (Yt.call(this, o - this.tokenStart + 2, Ge), this.next(), n = this.substrToCursor(o + 1)));
      } else
        this.error();
      return t !== null && t.charCodeAt(0) === Ee && (t = t.substr(1)), n !== null && n.charCodeAt(0) === Ee && (n = n.substr(1)), {
        type: "AnPlusB",
        loc: this.getLocation(e, this.tokenStart),
        a: t,
        b: n
      };
    }
    function tu(e) {
      if (e.a) {
        const t = e.a === "+1" && "n" || e.a === "1" && "n" || e.a === "-1" && "-n" || e.a + "n";
        if (e.b) {
          const n = e.b[0] === "-" || e.b[0] === "+" ? e.b : "+" + e.b;
          this.tokenize(t + n);
        } else
          this.tokenize(t);
      } else
        this.tokenize(e.b);
    }
    const nu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: tu,
      name: Jc,
      parse: Ko,
      structure: eu
    }, Symbol.toStringTag, { value: "Module" }));
    function Ti() {
      return this.Raw(this.consumeUntilLeftCurlyBracketOrSemicolon, !0);
    }
    function ru() {
      for (let e = 1, t; t = this.lookupType(e); e++) {
        if (t === ue)
          return !0;
        if (t === V || t === G)
          return !1;
      }
      return !1;
    }
    const iu = "Atrule", ou = "atrule", su = {
      name: String,
      prelude: ["AtrulePrelude", "Raw", null],
      block: ["Block", null]
    };
    function Yo(e = !1) {
      const t = this.tokenStart;
      let n, r, i = null, o = null;
      switch (this.eat(G), n = this.substrToCursor(t + 1), r = n.toLowerCase(), this.skipSC(), this.eof === !1 && this.tokenType !== V && this.tokenType !== re && (this.parseAtrulePrelude ? i = this.parseWithFallback(this.AtrulePrelude.bind(this, n, e), Ti) : i = Ti.call(this, this.tokenIndex), this.skipSC()), this.tokenType) {
        case re:
          this.next();
          break;
        case V:
          this.eat(V), hasOwnProperty.call(this.atrule, r) && typeof this.atrule[r].block == "function" ? o = this.atrule[r].block.call(this, e) : o = this.Block(ru.call(this)), this.eof || this.eat(ue);
          break;
      }
      return {
        type: "Atrule",
        loc: this.getLocation(t, this.tokenStart),
        name: n,
        prelude: i,
        block: o
      };
    }
    function au(e) {
      this.token(G, "@" + e.name), e.prelude !== null && this.node(e.prelude), e.block ? (this.token(V, "{"), this.node(e.block), this.token(ue, "}")) : this.token(re, ";");
    }
    const lu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: au,
      name: iu,
      parse: Yo,
      structure: su,
      walkContext: ou
    }, Symbol.toStringTag, { value: "Module" })), cu = "AtrulePrelude", uu = "atrulePrelude", hu = {
      children: [[]]
    };
    function Qo(e) {
      let t = null;
      return e !== null && (e = e.toLowerCase()), this.skipSC(), hasOwnProperty.call(this.atrule, e) && typeof this.atrule[e].prelude == "function" ? t = this.atrule[e].prelude.call(this) : t = this.readSequence(this.scope.AtrulePrelude), this.skipSC(), this.eof !== !0 && this.tokenType !== V && this.tokenType !== re && this.error("Semicolon or block is expected"), {
        type: "AtrulePrelude",
        loc: this.getLocationFromList(t),
        children: t
      };
    }
    function fu(e) {
      this.children(e);
    }
    const pu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: fu,
      name: cu,
      parse: Qo,
      structure: hu,
      walkContext: uu
    }, Symbol.toStringTag, { value: "Module" })), du = 36, Xo = 42, Qt = 61, mu = 94, rr = 124, gu = 126;
    function bu() {
      this.eof && this.error("Unexpected end of input");
      const e = this.tokenStart;
      let t = !1;
      return this.isDelim(Xo) ? (t = !0, this.next()) : this.isDelim(rr) || this.eat(y), this.isDelim(rr) ? this.charCodeAt(this.tokenStart + 1) !== Qt ? (this.next(), this.eat(y)) : t && this.error("Identifier is expected", this.tokenEnd) : t && this.error("Vertical line is expected"), {
        type: "Identifier",
        loc: this.getLocation(e, this.tokenStart),
        name: this.substrToCursor(e)
      };
    }
    function yu() {
      const e = this.tokenStart, t = this.charCodeAt(e);
      return t !== Qt && // =
      t !== gu && // ~=
      t !== mu && // ^=
      t !== du && // $=
      t !== Xo && // *=
      t !== rr && this.error("Attribute selector (=, ~=, ^=, $=, *=, |=) is expected"), this.next(), t !== Qt && (this.isDelim(Qt) || this.error("Equal sign is expected"), this.next()), this.substrToCursor(e);
    }
    const ku = "AttributeSelector", xu = {
      name: "Identifier",
      matcher: [String, null],
      value: ["String", "Identifier", null],
      flags: [String, null]
    };
    function Zo() {
      const e = this.tokenStart;
      let t, n = null, r = null, i = null;
      return this.eat(pe), this.skipSC(), t = bu.call(this), this.skipSC(), this.tokenType !== Se && (this.tokenType !== y && (n = yu.call(this), this.skipSC(), r = this.tokenType === Te ? this.String() : this.Identifier(), this.skipSC()), this.tokenType === y && (i = this.consume(y), this.skipSC())), this.eat(Se), {
        type: "AttributeSelector",
        loc: this.getLocation(e, this.tokenStart),
        name: t,
        matcher: n,
        value: r,
        flags: i
      };
    }
    function wu(e) {
      this.token(I, "["), this.node(e.name), e.matcher !== null && (this.tokenize(e.matcher), this.node(e.value)), e.flags !== null && this.token(y, e.flags), this.token(I, "]");
    }
    const vu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: wu,
      name: ku,
      parse: Zo,
      structure: xu
    }, Symbol.toStringTag, { value: "Module" })), Su = 38;
    function Jo() {
      return this.Raw(null, !0);
    }
    function Ai() {
      return this.parseWithFallback(this.Rule, Jo);
    }
    function Oi() {
      return this.Raw(this.consumeUntilSemicolonIncluded, !0);
    }
    function Cu() {
      if (this.tokenType === re)
        return Oi.call(this, this.tokenIndex);
      const e = this.parseWithFallback(this.Declaration, Oi);
      return this.tokenType === re && this.next(), e;
    }
    const Tu = "Block", Au = "block", Ou = {
      children: [[
        "Atrule",
        "Rule",
        "Declaration"
      ]]
    };
    function es(e) {
      const t = e ? Cu : Ai, n = this.tokenStart;
      let r = this.createList();
      e:
        for (; !this.eof; )
          switch (this.tokenType) {
            case ue:
              break e;
            case W:
            case X:
              this.next();
              break;
            case G:
              r.push(this.parseWithFallback(this.Atrule.bind(this, e), Jo));
              break;
            default:
              e && this.isDelim(Su) ? r.push(Ai.call(this)) : r.push(t.call(this));
          }
      return {
        type: "Block",
        loc: this.getLocation(n, this.tokenStart),
        children: r
      };
    }
    function Eu(e) {
      this.children(e, (t) => {
        t.type === "Declaration" && this.token(re, ";");
      });
    }
    const Lu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Eu,
      name: Tu,
      parse: es,
      structure: Ou,
      walkContext: Au
    }, Symbol.toStringTag, { value: "Module" })), $u = "Brackets", _u = {
      children: [[]]
    };
    function ts(e, t) {
      const n = this.tokenStart;
      let r = null;
      return this.eat(pe), r = e.call(this, t), this.eof || this.eat(Se), {
        type: "Brackets",
        loc: this.getLocation(n, this.tokenStart),
        children: r
      };
    }
    function Pu(e) {
      this.token(I, "["), this.children(e), this.token(I, "]");
    }
    const zu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Pu,
      name: $u,
      parse: ts,
      structure: _u
    }, Symbol.toStringTag, { value: "Module" })), Iu = "CDC", Ru = [];
    function ns() {
      const e = this.tokenStart;
      return this.eat(ae), {
        type: "CDC",
        loc: this.getLocation(e, this.tokenStart)
      };
    }
    function Mu() {
      this.token(ae, "-->");
    }
    const Nu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Mu,
      name: Iu,
      parse: ns,
      structure: Ru
    }, Symbol.toStringTag, { value: "Module" })), Du = "CDO", ju = [];
    function rs() {
      const e = this.tokenStart;
      return this.eat(Rt), {
        type: "CDO",
        loc: this.getLocation(e, this.tokenStart)
      };
    }
    function Fu() {
      this.token(Rt, "<!--");
    }
    const Bu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Fu,
      name: Du,
      parse: rs,
      structure: ju
    }, Symbol.toStringTag, { value: "Module" })), Wu = 46, Hu = "ClassSelector", Uu = {
      name: String
    };
    function is() {
      return this.eatDelim(Wu), {
        type: "ClassSelector",
        loc: this.getLocation(this.tokenStart - 1, this.tokenEnd),
        name: this.consume(y)
      };
    }
    function qu(e) {
      this.token(I, "."), this.token(y, e.name);
    }
    const Gu = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: qu,
      name: Hu,
      parse: is,
      structure: Uu
    }, Symbol.toStringTag, { value: "Module" })), Vu = 43, Ei = 47, Ku = 62, Yu = 126, Qu = "Combinator", Xu = {
      name: String
    };
    function os() {
      const e = this.tokenStart;
      let t;
      switch (this.tokenType) {
        case W:
          t = " ";
          break;
        case I:
          switch (this.charCodeAt(this.tokenStart)) {
            case Ku:
            case Vu:
            case Yu:
              this.next();
              break;
            case Ei:
              this.next(), this.eatIdent("deep"), this.eatDelim(Ei);
              break;
            default:
              this.error("Combinator is expected");
          }
          t = this.substrToCursor(e);
          break;
      }
      return {
        type: "Combinator",
        loc: this.getLocation(e, this.tokenStart),
        name: t
      };
    }
    function Zu(e) {
      this.tokenize(e.name);
    }
    const Ju = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Zu,
      name: Qu,
      parse: os,
      structure: Xu
    }, Symbol.toStringTag, { value: "Module" })), eh = 42, th = 47, nh = "Comment", rh = {
      value: String
    };
    function ss() {
      const e = this.tokenStart;
      let t = this.tokenEnd;
      return this.eat(X), t - e + 2 >= 2 && this.charCodeAt(t - 2) === eh && this.charCodeAt(t - 1) === th && (t -= 2), {
        type: "Comment",
        loc: this.getLocation(e, this.tokenStart),
        value: this.substring(e + 2, t)
      };
    }
    function ih(e) {
      this.token(X, "/*" + e.value + "*/");
    }
    const oh = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: ih,
      name: nh,
      parse: ss,
      structure: rh
    }, Symbol.toStringTag, { value: "Module" })), sh = /* @__PURE__ */ new Set([ne, E, De]), ah = "Condition", lh = {
      kind: String,
      children: [[
        "Identifier",
        "Feature",
        "FeatureFunction",
        "FeatureRange",
        "SupportsDeclaration"
      ]]
    };
    function Li(e) {
      return this.lookupTypeNonSC(1) === y && sh.has(this.lookupTypeNonSC(2)) ? this.Feature(e) : this.FeatureRange(e);
    }
    const ch = {
      media: Li,
      container: Li,
      supports() {
        return this.SupportsDeclaration();
      }
    };
    function as(e = "media") {
      const t = this.createList();
      e: for (; !this.eof; )
        switch (this.tokenType) {
          case X:
          case W:
            this.next();
            continue;
          case y:
            t.push(this.Identifier());
            break;
          case M: {
            let n = this.parseWithFallback(
              () => ch[e].call(this, e),
              () => null
            );
            n || (n = this.parseWithFallback(
              () => {
                this.eat(M);
                const r = this.Condition(e);
                return this.eat(E), r;
              },
              () => this.GeneralEnclosed(e)
            )), t.push(n);
            break;
          }
          case $: {
            let n = this.parseWithFallback(
              () => this.FeatureFunction(e),
              () => null
            );
            n || (n = this.GeneralEnclosed(e)), t.push(n);
            break;
          }
          default:
            break e;
        }
      return t.isEmpty && this.error("Condition is expected"), {
        type: "Condition",
        loc: this.getLocationFromList(t),
        kind: e,
        children: t
      };
    }
    function uh(e) {
      e.children.forEach((t) => {
        t.type === "Condition" ? (this.token(M, "("), this.node(t), this.token(E, ")")) : this.node(t);
      });
    }
    const hh = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: uh,
      name: ah,
      parse: as,
      structure: lh
    }, Symbol.toStringTag, { value: "Module" })), ls = 33, fh = 35, ph = 36, dh = 38, mh = 42, gh = 43, $i = 47;
    function bh() {
      return this.Raw(this.consumeUntilExclamationMarkOrSemicolon, !0);
    }
    function yh() {
      return this.Raw(this.consumeUntilExclamationMarkOrSemicolon, !1);
    }
    function kh() {
      const e = this.tokenIndex, t = this.Value();
      return t.type !== "Raw" && this.eof === !1 && this.tokenType !== re && this.isDelim(ls) === !1 && this.isBalanceEdge(e) === !1 && this.error(), t;
    }
    const xh = "Declaration", wh = "declaration", vh = {
      important: [Boolean, String],
      property: String,
      value: ["Value", "Raw"]
    };
    function cs() {
      const e = this.tokenStart, t = this.tokenIndex, n = Ch.call(this), r = Cr(n), i = r ? this.parseCustomProperty : this.parseValue, o = r ? yh : bh;
      let s = !1, c;
      this.skipSC(), this.eat(ne);
      const l = this.tokenIndex;
      if (r || this.skipSC(), i ? c = this.parseWithFallback(kh, o) : c = o.call(this, this.tokenIndex), r && c.type === "Value" && c.children.isEmpty) {
        for (let a = l - this.tokenIndex; a <= 0; a++)
          if (this.lookupType(a) === W) {
            c.children.appendData({
              type: "WhiteSpace",
              loc: null,
              value: " "
            });
            break;
          }
      }
      return this.isDelim(ls) && (s = Th.call(this), this.skipSC()), this.eof === !1 && this.tokenType !== re && this.isBalanceEdge(t) === !1 && this.error(), {
        type: "Declaration",
        loc: this.getLocation(e, this.tokenStart),
        important: s,
        property: n,
        value: c
      };
    }
    function Sh(e) {
      this.token(y, e.property), this.token(ne, ":"), this.node(e.value), e.important && (this.token(I, "!"), this.token(y, e.important === !0 ? "important" : e.important));
    }
    function Ch() {
      const e = this.tokenStart;
      if (this.tokenType === I)
        switch (this.charCodeAt(this.tokenStart)) {
          case mh:
          case ph:
          case gh:
          case fh:
          case dh:
            this.next();
            break;
          case $i:
            this.next(), this.isDelim($i) && this.next();
            break;
        }
      return this.tokenType === F ? this.eat(F) : this.eat(y), this.substrToCursor(e);
    }
    function Th() {
      this.eat(I), this.skipSC();
      const e = this.consume(y);
      return e === "important" ? !0 : e;
    }
    const Ah = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Sh,
      name: xh,
      parse: cs,
      structure: vh,
      walkContext: wh
    }, Symbol.toStringTag, { value: "Module" })), Oh = 38;
    function Fn() {
      return this.Raw(this.consumeUntilSemicolonIncluded, !0);
    }
    const Eh = "DeclarationList", Lh = {
      children: [[
        "Declaration",
        "Atrule",
        "Rule"
      ]]
    };
    function us() {
      const e = this.createList();
      for (; !this.eof; )
        switch (this.tokenType) {
          case W:
          case X:
          case re:
            this.next();
            break;
          case G:
            e.push(this.parseWithFallback(this.Atrule.bind(this, !0), Fn));
            break;
          default:
            this.isDelim(Oh) ? e.push(this.parseWithFallback(this.Rule, Fn)) : e.push(this.parseWithFallback(this.Declaration, Fn));
        }
      return {
        type: "DeclarationList",
        loc: this.getLocationFromList(e),
        children: e
      };
    }
    function $h(e) {
      this.children(e, (t) => {
        t.type === "Declaration" && this.token(re, ";");
      });
    }
    const _h = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: $h,
      name: Eh,
      parse: us,
      structure: Lh
    }, Symbol.toStringTag, { value: "Module" })), Ph = "Dimension", zh = {
      value: String,
      unit: String
    };
    function hs() {
      const e = this.tokenStart, t = this.consumeNumber(z);
      return {
        type: "Dimension",
        loc: this.getLocation(e, this.tokenStart),
        value: t,
        unit: this.substring(e + t.length, this.tokenStart)
      };
    }
    function Ih(e) {
      this.token(z, e.value + e.unit);
    }
    const Rh = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Ih,
      name: Ph,
      parse: hs,
      structure: zh
    }, Symbol.toStringTag, { value: "Module" })), Mh = 47, Nh = "Feature", Dh = {
      kind: String,
      name: String,
      value: ["Identifier", "Number", "Dimension", "Ratio", "Function", null]
    };
    function fs(e) {
      const t = this.tokenStart;
      let n, r = null;
      if (this.eat(M), this.skipSC(), n = this.consume(y), this.skipSC(), this.tokenType !== E) {
        switch (this.eat(ne), this.skipSC(), this.tokenType) {
          case L:
            this.lookupNonWSType(1) === I ? r = this.Ratio() : r = this.Number();
            break;
          case z:
            r = this.Dimension();
            break;
          case y:
            r = this.Identifier();
            break;
          case $:
            r = this.parseWithFallback(
              () => {
                const i = this.Function(this.readSequence, this.scope.Value);
                return this.skipSC(), this.isDelim(Mh) && this.error(), i;
              },
              () => this.Ratio()
            );
            break;
          default:
            this.error("Number, dimension, ratio or identifier is expected");
        }
        this.skipSC();
      }
      return this.eof || this.eat(E), {
        type: "Feature",
        loc: this.getLocation(t, this.tokenStart),
        kind: e,
        name: n,
        value: r
      };
    }
    function jh(e) {
      this.token(M, "("), this.token(y, e.name), e.value !== null && (this.token(ne, ":"), this.node(e.value)), this.token(E, ")");
    }
    const Fh = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: jh,
      name: Nh,
      parse: fs,
      structure: Dh
    }, Symbol.toStringTag, { value: "Module" })), Bh = "FeatureFunction", Wh = {
      kind: String,
      feature: String,
      value: ["Declaration", "Selector"]
    };
    function Hh(e, t) {
      const r = (this.features[e] || {})[t];
      return typeof r != "function" && this.error(`Unknown feature ${t}()`), r;
    }
    function ps(e = "unknown") {
      const t = this.tokenStart, n = this.consumeFunctionName(), r = Hh.call(this, e, n.toLowerCase());
      this.skipSC();
      const i = this.parseWithFallback(
        () => {
          const o = this.tokenIndex, s = r.call(this);
          return this.eof === !1 && this.isBalanceEdge(o) === !1 && this.error(), s;
        },
        () => this.Raw(null, !1)
      );
      return this.eof || this.eat(E), {
        type: "FeatureFunction",
        loc: this.getLocation(t, this.tokenStart),
        kind: e,
        feature: n,
        value: i
      };
    }
    function Uh(e) {
      this.token($, e.feature + "("), this.node(e.value), this.token(E, ")");
    }
    const qh = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Uh,
      name: Bh,
      parse: ps,
      structure: Wh
    }, Symbol.toStringTag, { value: "Module" })), _i = 47, Gh = 60, Pi = 61, Vh = 62, Kh = "FeatureRange", Yh = {
      kind: String,
      left: ["Identifier", "Number", "Dimension", "Ratio", "Function"],
      leftComparison: String,
      middle: ["Identifier", "Number", "Dimension", "Ratio", "Function"],
      rightComparison: [String, null],
      right: ["Identifier", "Number", "Dimension", "Ratio", "Function", null]
    };
    function Bn() {
      switch (this.skipSC(), this.tokenType) {
        case L:
          return this.isDelim(_i, this.lookupOffsetNonSC(1)) ? this.Ratio() : this.Number();
        case z:
          return this.Dimension();
        case y:
          return this.Identifier();
        case $:
          return this.parseWithFallback(
            () => {
              const e = this.Function(this.readSequence, this.scope.Value);
              return this.skipSC(), this.isDelim(_i) && this.error(), e;
            },
            () => this.Ratio()
          );
        default:
          this.error("Number, dimension, ratio or identifier is expected");
      }
    }
    function zi(e) {
      if (this.skipSC(), this.isDelim(Gh) || this.isDelim(Vh)) {
        const t = this.source[this.tokenStart];
        return this.next(), this.isDelim(Pi) ? (this.next(), t + "=") : t;
      }
      if (this.isDelim(Pi))
        return "=";
      this.error(`Expected ${e ? '":", ' : ""}"<", ">", "=" or ")"`);
    }
    function ds(e = "unknown") {
      const t = this.tokenStart;
      this.skipSC(), this.eat(M);
      const n = Bn.call(this), r = zi.call(this, n.type === "Identifier"), i = Bn.call(this);
      let o = null, s = null;
      return this.lookupNonWSType(0) !== E && (o = zi.call(this), s = Bn.call(this)), this.skipSC(), this.eat(E), {
        type: "FeatureRange",
        loc: this.getLocation(t, this.tokenStart),
        kind: e,
        left: n,
        leftComparison: r,
        middle: i,
        rightComparison: o,
        right: s
      };
    }
    function Qh(e) {
      this.token(M, "("), this.node(e.left), this.tokenize(e.leftComparison), this.node(e.middle), e.right && (this.tokenize(e.rightComparison), this.node(e.right)), this.token(E, ")");
    }
    const Xh = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Qh,
      name: Kh,
      parse: ds,
      structure: Yh
    }, Symbol.toStringTag, { value: "Module" })), Zh = "Function", Jh = "function", ef = {
      name: String,
      children: [[]]
    };
    function ms(e, t) {
      const n = this.tokenStart, r = this.consumeFunctionName(), i = r.toLowerCase();
      let o;
      return o = t.hasOwnProperty(i) ? t[i].call(this, t) : e.call(this, t), this.eof || this.eat(E), {
        type: "Function",
        loc: this.getLocation(n, this.tokenStart),
        name: r,
        children: o
      };
    }
    function tf(e) {
      this.token($, e.name + "("), this.children(e), this.token(E, ")");
    }
    const nf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: tf,
      name: Zh,
      parse: ms,
      structure: ef,
      walkContext: Jh
    }, Symbol.toStringTag, { value: "Module" })), rf = "GeneralEnclosed", of = {
      kind: String,
      function: [String, null],
      children: [[]]
    };
    function gs(e) {
      const t = this.tokenStart;
      let n = null;
      this.tokenType === $ ? n = this.consumeFunctionName() : this.eat(M);
      const r = this.parseWithFallback(
        () => {
          const i = this.tokenIndex, o = this.readSequence(this.scope.Value);
          return this.eof === !1 && this.isBalanceEdge(i) === !1 && this.error(), o;
        },
        () => this.createSingleNodeList(
          this.Raw(null, !1)
        )
      );
      return this.eof || this.eat(E), {
        type: "GeneralEnclosed",
        loc: this.getLocation(t, this.tokenStart),
        kind: e,
        function: n,
        children: r
      };
    }
    function sf(e) {
      e.function ? this.token($, e.function + "(") : this.token(M, "("), this.children(e), this.token(E, ")");
    }
    const af = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: sf,
      name: rf,
      parse: gs,
      structure: of
    }, Symbol.toStringTag, { value: "Module" })), lf = "XXX", cf = "Hash", uf = {
      value: String
    };
    function bs() {
      const e = this.tokenStart;
      return this.eat(F), {
        type: "Hash",
        loc: this.getLocation(e, this.tokenStart),
        value: this.substrToCursor(e + 1)
      };
    }
    function hf(e) {
      this.token(F, "#" + e.value);
    }
    const ff = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: hf,
      name: cf,
      parse: bs,
      structure: uf,
      xxx: lf
    }, Symbol.toStringTag, { value: "Module" })), pf = "Identifier", df = {
      name: String
    };
    function ys() {
      return {
        type: "Identifier",
        loc: this.getLocation(this.tokenStart, this.tokenEnd),
        name: this.consume(y)
      };
    }
    function mf(e) {
      this.token(y, e.name);
    }
    const gf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: mf,
      name: pf,
      parse: ys,
      structure: df
    }, Symbol.toStringTag, { value: "Module" })), bf = "IdSelector", yf = {
      name: String
    };
    function ks() {
      const e = this.tokenStart;
      return this.eat(F), {
        type: "IdSelector",
        loc: this.getLocation(e, this.tokenStart),
        name: this.substrToCursor(e + 1)
      };
    }
    function kf(e) {
      this.token(I, "#" + e.name);
    }
    const xf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: kf,
      name: bf,
      parse: ks,
      structure: yf
    }, Symbol.toStringTag, { value: "Module" })), wf = 46, vf = "Layer", Sf = {
      name: String
    };
    function xs() {
      let e = this.consume(y);
      for (; this.isDelim(wf); )
        this.eat(I), e += "." + this.consume(y);
      return {
        type: "Layer",
        loc: this.getLocation(this.tokenStart, this.tokenEnd),
        name: e
      };
    }
    function Cf(e) {
      this.tokenize(e.name);
    }
    const Tf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Cf,
      name: vf,
      parse: xs,
      structure: Sf
    }, Symbol.toStringTag, { value: "Module" })), Af = "LayerList", Of = {
      children: [[
        "Layer"
      ]]
    };
    function ws() {
      const e = this.createList();
      for (this.skipSC(); !this.eof && (e.push(this.Layer()), this.lookupTypeNonSC(0) === ce); )
        this.skipSC(), this.next(), this.skipSC();
      return {
        type: "LayerList",
        loc: this.getLocationFromList(e),
        children: e
      };
    }
    function Ef(e) {
      this.children(e, () => this.token(ce, ","));
    }
    const Lf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Ef,
      name: Af,
      parse: ws,
      structure: Of
    }, Symbol.toStringTag, { value: "Module" })), $f = "MediaQuery", _f = {
      modifier: [String, null],
      mediaType: [String, null],
      condition: ["Condition", null]
    };
    function vs() {
      const e = this.tokenStart;
      let t = null, n = null, r = null;
      if (this.skipSC(), this.tokenType === y && this.lookupTypeNonSC(1) !== M) {
        const i = this.consume(y), o = i.toLowerCase();
        switch (o === "not" || o === "only" ? (this.skipSC(), t = o, n = this.consume(y)) : n = i, this.lookupTypeNonSC(0)) {
          case y: {
            this.skipSC(), this.eatIdent("and"), r = this.Condition("media");
            break;
          }
          case V:
          case re:
          case ce:
          case De:
            break;
          default:
            this.error("Identifier or parenthesis is expected");
        }
      } else
        switch (this.tokenType) {
          case y:
          case M:
          case $: {
            r = this.Condition("media");
            break;
          }
          case V:
          case re:
          case De:
            break;
          default:
            this.error("Identifier or parenthesis is expected");
        }
      return {
        type: "MediaQuery",
        loc: this.getLocation(e, this.tokenStart),
        modifier: t,
        mediaType: n,
        condition: r
      };
    }
    function Pf(e) {
      e.mediaType ? (e.modifier && this.token(y, e.modifier), this.token(y, e.mediaType), e.condition && (this.token(y, "and"), this.node(e.condition))) : e.condition && this.node(e.condition);
    }
    const zf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Pf,
      name: $f,
      parse: vs,
      structure: _f
    }, Symbol.toStringTag, { value: "Module" })), If = "MediaQueryList", Rf = {
      children: [[
        "MediaQuery"
      ]]
    };
    function Ss() {
      const e = this.createList();
      for (this.skipSC(); !this.eof && (e.push(this.MediaQuery()), this.tokenType === ce); )
        this.next();
      return {
        type: "MediaQueryList",
        loc: this.getLocationFromList(e),
        children: e
      };
    }
    function Mf(e) {
      this.children(e, () => this.token(ce, ","));
    }
    const Nf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Mf,
      name: If,
      parse: Ss,
      structure: Rf
    }, Symbol.toStringTag, { value: "Module" })), Df = 38, jf = "NestingSelector", Ff = {};
    function Cs() {
      const e = this.tokenStart;
      return this.eatDelim(Df), {
        type: "NestingSelector",
        loc: this.getLocation(e, this.tokenStart)
      };
    }
    function Bf() {
      this.token(I, "&");
    }
    const Wf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Bf,
      name: jf,
      parse: Cs,
      structure: Ff
    }, Symbol.toStringTag, { value: "Module" })), Hf = "Nth", Uf = {
      nth: ["AnPlusB", "Identifier"],
      selector: ["SelectorList", null]
    };
    function Ts() {
      this.skipSC();
      const e = this.tokenStart;
      let t = e, n = null, r;
      return this.lookupValue(0, "odd") || this.lookupValue(0, "even") ? r = this.Identifier() : r = this.AnPlusB(), t = this.tokenStart, this.skipSC(), this.lookupValue(0, "of") && (this.next(), n = this.SelectorList(), t = this.tokenStart), {
        type: "Nth",
        loc: this.getLocation(e, t),
        nth: r,
        selector: n
      };
    }
    function qf(e) {
      this.node(e.nth), e.selector !== null && (this.token(y, "of"), this.node(e.selector));
    }
    const Gf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: qf,
      name: Hf,
      parse: Ts,
      structure: Uf
    }, Symbol.toStringTag, { value: "Module" })), Vf = "Number", Kf = {
      value: String
    };
    function As() {
      return {
        type: "Number",
        loc: this.getLocation(this.tokenStart, this.tokenEnd),
        value: this.consume(L)
      };
    }
    function Yf(e) {
      this.token(L, e.value);
    }
    const Qf = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Yf,
      name: Vf,
      parse: As,
      structure: Kf
    }, Symbol.toStringTag, { value: "Module" })), Xf = "Operator", Zf = {
      value: String
    };
    function Os() {
      const e = this.tokenStart;
      return this.next(), {
        type: "Operator",
        loc: this.getLocation(e, this.tokenStart),
        value: this.substrToCursor(e)
      };
    }
    function Jf(e) {
      this.tokenize(e.value);
    }
    const ep = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Jf,
      name: Xf,
      parse: Os,
      structure: Zf
    }, Symbol.toStringTag, { value: "Module" })), tp = "Parentheses", np = {
      children: [[]]
    };
    function Es(e, t) {
      const n = this.tokenStart;
      let r = null;
      return this.eat(M), r = e.call(this, t), this.eof || this.eat(E), {
        type: "Parentheses",
        loc: this.getLocation(n, this.tokenStart),
        children: r
      };
    }
    function rp(e) {
      this.token(M, "("), this.children(e), this.token(E, ")");
    }
    const ip = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: rp,
      name: tp,
      parse: Es,
      structure: np
    }, Symbol.toStringTag, { value: "Module" })), op = "Percentage", sp = {
      value: String
    };
    function Ls() {
      return {
        type: "Percentage",
        loc: this.getLocation(this.tokenStart, this.tokenEnd),
        value: this.consumeNumber(B)
      };
    }
    function ap(e) {
      this.token(B, e.value + "%");
    }
    const lp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: ap,
      name: op,
      parse: Ls,
      structure: sp
    }, Symbol.toStringTag, { value: "Module" })), cp = "PseudoClassSelector", up = "function", hp = {
      name: String,
      children: [["Raw"], null]
    };
    function $s() {
      const e = this.tokenStart;
      let t = null, n, r;
      return this.eat(ne), this.tokenType === $ ? (n = this.consumeFunctionName(), r = n.toLowerCase(), this.lookupNonWSType(0) == E ? t = this.createList() : hasOwnProperty.call(this.pseudo, r) ? (this.skipSC(), t = this.pseudo[r].call(this), this.skipSC()) : (t = this.createList(), t.push(
        this.Raw(null, !1)
      )), this.eat(E)) : n = this.consume(y), {
        type: "PseudoClassSelector",
        loc: this.getLocation(e, this.tokenStart),
        name: n,
        children: t
      };
    }
    function fp(e) {
      this.token(ne, ":"), e.children === null ? this.token(y, e.name) : (this.token($, e.name + "("), this.children(e), this.token(E, ")"));
    }
    const pp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: fp,
      name: cp,
      parse: $s,
      structure: hp,
      walkContext: up
    }, Symbol.toStringTag, { value: "Module" })), dp = "PseudoElementSelector", mp = "function", gp = {
      name: String,
      children: [["Raw"], null]
    };
    function _s() {
      const e = this.tokenStart;
      let t = null, n, r;
      return this.eat(ne), this.eat(ne), this.tokenType === $ ? (n = this.consumeFunctionName(), r = n.toLowerCase(), this.lookupNonWSType(0) == E ? t = this.createList() : hasOwnProperty.call(this.pseudo, r) ? (this.skipSC(), t = this.pseudo[r].call(this), this.skipSC()) : (t = this.createList(), t.push(
        this.Raw(null, !1)
      )), this.eat(E)) : n = this.consume(y), {
        type: "PseudoElementSelector",
        loc: this.getLocation(e, this.tokenStart),
        name: n,
        children: t
      };
    }
    function bp(e) {
      this.token(ne, ":"), this.token(ne, ":"), e.children === null ? this.token(y, e.name) : (this.token($, e.name + "("), this.children(e), this.token(E, ")"));
    }
    const yp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: bp,
      name: dp,
      parse: _s,
      structure: gp,
      walkContext: mp
    }, Symbol.toStringTag, { value: "Module" })), Ii = 47;
    function Ri() {
      switch (this.skipSC(), this.tokenType) {
        case L:
          return this.Number();
        case $:
          return this.Function(this.readSequence, this.scope.Value);
        default:
          this.error("Number of function is expected");
      }
    }
    const kp = "Ratio", xp = {
      left: ["Number", "Function"],
      right: ["Number", "Function", null]
    };
    function Ps() {
      const e = this.tokenStart, t = Ri.call(this);
      let n = null;
      return this.skipSC(), this.isDelim(Ii) && (this.eatDelim(Ii), n = Ri.call(this)), {
        type: "Ratio",
        loc: this.getLocation(e, this.tokenStart),
        left: t,
        right: n
      };
    }
    function wp(e) {
      this.node(e.left), this.token(I, "/"), e.right ? this.node(e.right) : this.node(L, 1);
    }
    const vp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: wp,
      name: kp,
      parse: Ps,
      structure: xp
    }, Symbol.toStringTag, { value: "Module" }));
    function Sp() {
      return this.tokenIndex > 0 && this.lookupType(-1) === W ? this.tokenIndex > 1 ? this.getTokenStart(this.tokenIndex - 1) : this.firstCharOffset : this.tokenStart;
    }
    const Cp = "Raw", Tp = {
      value: String
    };
    function zs(e, t) {
      const n = this.getTokenStart(this.tokenIndex);
      let r;
      return this.skipUntilBalanced(this.tokenIndex, e || this.consumeUntilBalanceEnd), t && this.tokenStart > n ? r = Sp.call(this) : r = this.tokenStart, {
        type: "Raw",
        loc: this.getLocation(n, r),
        value: this.substring(n, r)
      };
    }
    function Ap(e) {
      this.tokenize(e.value);
    }
    const Op = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Ap,
      name: Cp,
      parse: zs,
      structure: Tp
    }, Symbol.toStringTag, { value: "Module" }));
    function Mi() {
      return this.Raw(this.consumeUntilLeftCurlyBracket, !0);
    }
    function Ep() {
      const e = this.SelectorList();
      return e.type !== "Raw" && this.eof === !1 && this.tokenType !== V && this.error(), e;
    }
    const Lp = "Rule", $p = "rule", _p = {
      prelude: ["SelectorList", "Raw"],
      block: ["Block"]
    };
    function Is() {
      const e = this.tokenIndex, t = this.tokenStart;
      let n, r;
      return this.parseRulePrelude ? n = this.parseWithFallback(Ep, Mi) : n = Mi.call(this, e), this.skipSC(), this.eat(V), r = this.Block(!0), this.eof || this.eat(ue), {
        type: "Rule",
        loc: this.getLocation(t, this.tokenStart),
        prelude: n,
        block: r
      };
    }
    function Pp(e) {
      this.node(e.prelude), this.token(V, "{"), this.node(e.block), this.token(ue, "}");
    }
    const zp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Pp,
      name: Lp,
      parse: Is,
      structure: _p,
      walkContext: $p
    }, Symbol.toStringTag, { value: "Module" })), Ip = "Scope", Rp = {
      root: ["SelectorList", "Raw", null],
      limit: ["SelectorList", "Raw", null]
    };
    function Rs() {
      let e = null, t = null;
      this.skipSC();
      const n = this.tokenStart;
      return this.tokenType === M && (this.next(), this.skipSC(), e = this.parseWithFallback(
        this.SelectorList,
        () => this.Raw(!1, !0)
      ), this.skipSC(), this.eat(E)), this.lookupNonWSType(0) === y && (this.skipSC(), this.eatIdent("to"), this.skipSC(), this.eat(M), this.skipSC(), t = this.parseWithFallback(
        this.SelectorList,
        () => this.Raw(!1, !0)
      ), this.skipSC(), this.eat(E)), {
        type: "Scope",
        loc: this.getLocation(n, this.tokenStart),
        root: e,
        limit: t
      };
    }
    function Mp(e) {
      e.root && (this.token(M, "("), this.node(e.root), this.token(E, ")")), e.limit && (this.token(y, "to"), this.token(M, "("), this.node(e.limit), this.token(E, ")"));
    }
    const Np = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Mp,
      name: Ip,
      parse: Rs,
      structure: Rp
    }, Symbol.toStringTag, { value: "Module" })), Dp = "Selector", jp = {
      children: [[
        "TypeSelector",
        "IdSelector",
        "ClassSelector",
        "AttributeSelector",
        "PseudoClassSelector",
        "PseudoElementSelector",
        "Combinator"
      ]]
    };
    function Ms() {
      const e = this.readSequence(this.scope.Selector);
      return this.getFirstListNode(e) === null && this.error("Selector is expected"), {
        type: "Selector",
        loc: this.getLocationFromList(e),
        children: e
      };
    }
    function Fp(e) {
      this.children(e);
    }
    const Bp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Fp,
      name: Dp,
      parse: Ms,
      structure: jp
    }, Symbol.toStringTag, { value: "Module" })), Wp = "SelectorList", Hp = "selector", Up = {
      children: [[
        "Selector",
        "Raw"
      ]]
    };
    function Ns() {
      const e = this.createList();
      for (; !this.eof; ) {
        if (e.push(this.Selector()), this.tokenType === ce) {
          this.next();
          continue;
        }
        break;
      }
      return {
        type: "SelectorList",
        loc: this.getLocationFromList(e),
        children: e
      };
    }
    function qp(e) {
      this.children(e, () => this.token(ce, ","));
    }
    const Gp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: qp,
      name: Wp,
      parse: Ns,
      structure: Up,
      walkContext: Hp
    }, Symbol.toStringTag, { value: "Module" })), ir = 92, Ds = 34, Vp = 39;
    function js(e) {
      const t = e.length, n = e.charCodeAt(0), r = n === Ds || n === Vp ? 1 : 0, i = r === 1 && t > 1 && e.charCodeAt(t - 1) === n ? t - 2 : t - 1;
      let o = "";
      for (let s = r; s <= i; s++) {
        let c = e.charCodeAt(s);
        if (c === ir) {
          if (s === i) {
            s !== t - 1 && (o = e.substr(s + 1));
            break;
          }
          if (c = e.charCodeAt(++s), Le(ir, c)) {
            const l = s - 1, a = pt(e, l);
            s = a - 1, o += po(e.substring(l + 1, a));
          } else
            c === 13 && e.charCodeAt(s + 1) === 10 && s++;
        } else
          o += e[s];
      }
      return o;
    }
    function Kp(e, t) {
      const n = '"', r = Ds;
      let i = "", o = !1;
      for (let s = 0; s < e.length; s++) {
        const c = e.charCodeAt(s);
        if (c === 0) {
          i += "�";
          continue;
        }
        if (c <= 31 || c === 127) {
          i += "\\" + c.toString(16), o = !0;
          continue;
        }
        c === r || c === ir ? (i += "\\" + e.charAt(s), o = !1) : (o && (He(c) || Xe(c)) && (i += " "), i += e.charAt(s), o = !1);
      }
      return n + i + n;
    }
    const Yp = "String", Qp = {
      value: String
    };
    function Fs() {
      return {
        type: "String",
        loc: this.getLocation(this.tokenStart, this.tokenEnd),
        value: js(this.consume(Te))
      };
    }
    function Xp(e) {
      this.token(Te, Kp(e.value));
    }
    const Zp = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Xp,
      name: Yp,
      parse: Fs,
      structure: Qp
    }, Symbol.toStringTag, { value: "Module" })), Jp = 33;
    function Ni() {
      return this.Raw(null, !1);
    }
    const ed = "StyleSheet", td = "stylesheet", nd = {
      children: [[
        "Comment",
        "CDO",
        "CDC",
        "Atrule",
        "Rule",
        "Raw"
      ]]
    };
    function Bs() {
      const e = this.tokenStart, t = this.createList();
      let n;
      for (; !this.eof; ) {
        switch (this.tokenType) {
          case W:
            this.next();
            continue;
          case X:
            if (this.charCodeAt(this.tokenStart + 2) !== Jp) {
              this.next();
              continue;
            }
            n = this.Comment();
            break;
          case Rt:
            n = this.CDO();
            break;
          case ae:
            n = this.CDC();
            break;
          case G:
            n = this.parseWithFallback(this.Atrule, Ni);
            break;
          default:
            n = this.parseWithFallback(this.Rule, Ni);
        }
        t.push(n);
      }
      return {
        type: "StyleSheet",
        loc: this.getLocation(e, this.tokenStart),
        children: t
      };
    }
    function rd(e) {
      this.children(e);
    }
    const id = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: rd,
      name: ed,
      parse: Bs,
      structure: nd,
      walkContext: td
    }, Symbol.toStringTag, { value: "Module" })), od = "SupportsDeclaration", sd = {
      declaration: "Declaration"
    };
    function Ws() {
      const e = this.tokenStart;
      this.eat(M), this.skipSC();
      const t = this.Declaration();
      return this.eof || this.eat(E), {
        type: "SupportsDeclaration",
        loc: this.getLocation(e, this.tokenStart),
        declaration: t
      };
    }
    function ad(e) {
      this.token(M, "("), this.node(e.declaration), this.token(E, ")");
    }
    const ld = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: ad,
      name: od,
      parse: Ws,
      structure: sd
    }, Symbol.toStringTag, { value: "Module" })), cd = 42, Di = 124;
    function Wn() {
      this.tokenType !== y && this.isDelim(cd) === !1 && this.error("Identifier or asterisk is expected"), this.next();
    }
    const ud = "TypeSelector", hd = {
      name: String
    };
    function Hs() {
      const e = this.tokenStart;
      return this.isDelim(Di) ? (this.next(), Wn.call(this)) : (Wn.call(this), this.isDelim(Di) && (this.next(), Wn.call(this))), {
        type: "TypeSelector",
        loc: this.getLocation(e, this.tokenStart),
        name: this.substrToCursor(e)
      };
    }
    function fd(e) {
      this.tokenize(e.name);
    }
    const pd = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: fd,
      name: ud,
      parse: Hs,
      structure: hd
    }, Symbol.toStringTag, { value: "Module" })), Us = 43, qs = 45, or = 63;
    function Ct(e, t) {
      let n = 0;
      for (let r = this.tokenStart + e; r < this.tokenEnd; r++) {
        const i = this.charCodeAt(r);
        if (i === qs && t && n !== 0)
          return Ct.call(this, e + n + 1, !1), -1;
        He(i) || this.error(
          t && n !== 0 ? "Hyphen minus" + (n < 6 ? " or hex digit" : "") + " is expected" : n < 6 ? "Hex digit is expected" : "Unexpected input",
          r
        ), ++n > 6 && this.error("Too many hex digits", r);
      }
      return this.next(), n;
    }
    function Wt(e) {
      let t = 0;
      for (; this.isDelim(or); )
        ++t > e && this.error("Too many question marks"), this.next();
    }
    function dd(e) {
      this.charCodeAt(this.tokenStart) !== e && this.error((e === Us ? "Plus sign" : "Hyphen minus") + " is expected");
    }
    function md() {
      let e = 0;
      switch (this.tokenType) {
        case L:
          if (e = Ct.call(this, 1, !0), this.isDelim(or)) {
            Wt.call(this, 6 - e);
            break;
          }
          if (this.tokenType === z || this.tokenType === L) {
            dd.call(this, qs), Ct.call(this, 1, !1);
            break;
          }
          break;
        case z:
          e = Ct.call(this, 1, !0), e > 0 && Wt.call(this, 6 - e);
          break;
        default:
          if (this.eatDelim(Us), this.tokenType === y) {
            e = Ct.call(this, 0, !0), e > 0 && Wt.call(this, 6 - e);
            break;
          }
          if (this.isDelim(or)) {
            this.next(), Wt.call(this, 5);
            break;
          }
          this.error("Hex digit or question mark is expected");
      }
    }
    const gd = "UnicodeRange", bd = {
      value: String
    };
    function Gs() {
      const e = this.tokenStart;
      return this.eatIdent("u"), md.call(this), {
        type: "UnicodeRange",
        loc: this.getLocation(e, this.tokenStart),
        value: this.substrToCursor(e)
      };
    }
    function yd(e) {
      this.tokenize(e.value);
    }
    const kd = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: yd,
      name: gd,
      parse: Gs,
      structure: bd
    }, Symbol.toStringTag, { value: "Module" })), xd = 32, sr = 92, wd = 34, vd = 39, Sd = 40, Vs = 41;
    function Cd(e) {
      const t = e.length;
      let n = 4, r = e.charCodeAt(t - 1) === Vs ? t - 2 : t - 1, i = "";
      for (; n < r && Xe(e.charCodeAt(n)); )
        n++;
      for (; n < r && Xe(e.charCodeAt(r)); )
        r--;
      for (let o = n; o <= r; o++) {
        let s = e.charCodeAt(o);
        if (s === sr) {
          if (o === r) {
            o !== t - 1 && (i = e.substr(o + 1));
            break;
          }
          if (s = e.charCodeAt(++o), Le(sr, s)) {
            const c = o - 1, l = pt(e, c);
            o = l - 1, i += po(e.substring(c + 1, l));
          } else
            s === 13 && e.charCodeAt(o + 1) === 10 && o++;
        } else
          i += e[o];
      }
      return i;
    }
    function Td(e) {
      let t = "", n = !1;
      for (let r = 0; r < e.length; r++) {
        const i = e.charCodeAt(r);
        if (i === 0) {
          t += "�";
          continue;
        }
        if (i <= 31 || i === 127) {
          t += "\\" + i.toString(16), n = !0;
          continue;
        }
        i === xd || i === sr || i === wd || i === vd || i === Sd || i === Vs ? (t += "\\" + e.charAt(r), n = !1) : (n && He(i) && (t += " "), t += e.charAt(r), n = !1);
      }
      return "url(" + t + ")";
    }
    const Ad = "Url", Od = {
      value: String
    };
    function Ks() {
      const e = this.tokenStart;
      let t;
      switch (this.tokenType) {
        case te:
          t = Cd(this.consume(te));
          break;
        case $:
          this.cmpStr(this.tokenStart, this.tokenEnd, "url(") || this.error("Function name must be `url`"), this.eat($), this.skipSC(), t = js(this.consume(Te)), this.skipSC(), this.eof || this.eat(E);
          break;
        default:
          this.error("Url or Function is expected");
      }
      return {
        type: "Url",
        loc: this.getLocation(e, this.tokenStart),
        value: t
      };
    }
    function Ed(e) {
      this.token(te, Td(e.value));
    }
    const Ld = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Ed,
      name: Ad,
      parse: Ks,
      structure: Od
    }, Symbol.toStringTag, { value: "Module" })), $d = "Value", _d = {
      children: [[]]
    };
    function Ys() {
      const e = this.tokenStart, t = this.readSequence(this.scope.Value);
      return {
        type: "Value",
        loc: this.getLocation(e, this.tokenStart),
        children: t
      };
    }
    function Pd(e) {
      this.children(e);
    }
    const zd = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Pd,
      name: $d,
      parse: Ys,
      structure: _d
    }, Symbol.toStringTag, { value: "Module" })), Id = Object.freeze({
      type: "WhiteSpace",
      loc: null,
      value: " "
    }), Rd = "WhiteSpace", Md = {
      value: String
    };
    function Qs() {
      return this.eat(W), Id;
    }
    function Nd(e) {
      this.token(W, e.value);
    }
    const Dd = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      generate: Nd,
      name: Rd,
      parse: Qs,
      structure: Md
    }, Symbol.toStringTag, { value: "Module" })), Xs = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      AnPlusB: nu,
      Atrule: lu,
      AtrulePrelude: pu,
      AttributeSelector: vu,
      Block: Lu,
      Brackets: zu,
      CDC: Nu,
      CDO: Bu,
      ClassSelector: Gu,
      Combinator: Ju,
      Comment: oh,
      Condition: hh,
      Declaration: Ah,
      DeclarationList: _h,
      Dimension: Rh,
      Feature: Fh,
      FeatureFunction: qh,
      FeatureRange: Xh,
      Function: nf,
      GeneralEnclosed: af,
      Hash: ff,
      IdSelector: xf,
      Identifier: gf,
      Layer: Tf,
      LayerList: Lf,
      MediaQuery: zf,
      MediaQueryList: Nf,
      NestingSelector: Wf,
      Nth: Gf,
      Number: Qf,
      Operator: ep,
      Parentheses: ip,
      Percentage: lp,
      PseudoClassSelector: pp,
      PseudoElementSelector: yp,
      Ratio: vp,
      Raw: Op,
      Rule: zp,
      Scope: Np,
      Selector: Bp,
      SelectorList: Gp,
      String: Zp,
      StyleSheet: id,
      SupportsDeclaration: ld,
      TypeSelector: pd,
      UnicodeRange: kd,
      Url: Ld,
      Value: zd,
      WhiteSpace: Dd
    }, Symbol.toStringTag, { value: "Module" })), jd = Z(_({
      generic: !0
    }, Xc), {
      node: Xs
    }), Fd = 35, Bd = 42, ji = 43, Wd = 45, Hd = 47, Ud = 117;
    function Zs(e) {
      switch (this.tokenType) {
        case F:
          return this.Hash();
        case ce:
          return this.Operator();
        case M:
          return this.Parentheses(this.readSequence, e.recognizer);
        case pe:
          return this.Brackets(this.readSequence, e.recognizer);
        case Te:
          return this.String();
        case z:
          return this.Dimension();
        case B:
          return this.Percentage();
        case L:
          return this.Number();
        case $:
          return this.cmpStr(this.tokenStart, this.tokenEnd, "url(") ? this.Url() : this.Function(this.readSequence, e.recognizer);
        case te:
          return this.Url();
        case y:
          return this.cmpChar(this.tokenStart, Ud) && this.cmpChar(this.tokenStart + 1, ji) ? this.UnicodeRange() : this.Identifier();
        case I: {
          const t = this.charCodeAt(this.tokenStart);
          if (t === Hd || t === Bd || t === ji || t === Wd)
            return this.Operator();
          t === Fd && this.error("Hex or identifier is expected", this.tokenStart + 1);
          break;
        }
      }
    }
    const qd = {
      getNode: Zs
    }, Gd = 35, Vd = 38, Kd = 42, Yd = 43, Qd = 47, Fi = 46, Xd = 62, Zd = 124, Jd = 126;
    function em(e, t) {
      t.last !== null && t.last.type !== "Combinator" && e !== null && e.type !== "Combinator" && t.push({
        // FIXME: this.Combinator() should be used instead
        type: "Combinator",
        loc: null,
        name: " "
      });
    }
    function tm() {
      switch (this.tokenType) {
        case pe:
          return this.AttributeSelector();
        case F:
          return this.IdSelector();
        case ne:
          return this.lookupType(1) === ne ? this.PseudoElementSelector() : this.PseudoClassSelector();
        case y:
          return this.TypeSelector();
        case L:
        case B:
          return this.Percentage();
        case z:
          this.charCodeAt(this.tokenStart) === Fi && this.error("Identifier is expected", this.tokenStart + 1);
          break;
        case I: {
          switch (this.charCodeAt(this.tokenStart)) {
            case Yd:
            case Xd:
            case Jd:
            case Qd:
              return this.Combinator();
            case Fi:
              return this.ClassSelector();
            case Kd:
            case Zd:
              return this.TypeSelector();
            case Gd:
              return this.IdSelector();
            case Vd:
              return this.NestingSelector();
          }
          break;
        }
      }
    }
    const nm = {
      onWhiteSpace: em,
      getNode: tm
    };
    function rm() {
      return this.createSingleNodeList(
        this.Raw(null, !1)
      );
    }
    function im() {
      const e = this.createList();
      if (this.skipSC(), e.push(this.Identifier()), this.skipSC(), this.tokenType === ce) {
        e.push(this.Operator());
        const t = this.tokenIndex, n = this.parseCustomProperty ? this.Value(null) : this.Raw(this.consumeUntilExclamationMarkOrSemicolon, !1);
        if (n.type === "Value" && n.children.isEmpty) {
          for (let r = t - this.tokenIndex; r <= 0; r++)
            if (this.lookupType(r) === W) {
              n.children.appendData({
                type: "WhiteSpace",
                loc: null,
                value: " "
              });
              break;
            }
        }
        e.push(n);
      }
      return e;
    }
    function Bi(e) {
      return e !== null && e.type === "Operator" && (e.value[e.value.length - 1] === "-" || e.value[e.value.length - 1] === "+");
    }
    const om = {
      getNode: Zs,
      onWhiteSpace(e, t) {
        Bi(e) && (e.value = " " + e.value), Bi(t.last) && (t.last.value += " ");
      },
      expression: rm,
      var: im
    }, sm = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      AtrulePrelude: qd,
      Selector: nm,
      Value: om
    }, Symbol.toStringTag, { value: "Module" })), am = /* @__PURE__ */ new Set(["none", "and", "not", "or"]), lm = {
      parse: {
        prelude() {
          const e = this.createList();
          if (this.tokenType === y) {
            const t = this.substring(this.tokenStart, this.tokenEnd);
            am.has(t.toLowerCase()) || e.push(this.Identifier());
          }
          return e.push(this.Condition("container")), e;
        },
        block(e = !1) {
          return this.Block(e);
        }
      }
    }, cm = {
      parse: {
        prelude: null,
        block() {
          return this.Block(!0);
        }
      }
    };
    function Hn(e, t) {
      return this.parseWithFallback(
        () => {
          try {
            return e.call(this);
          } finally {
            this.skipSC(), this.lookupNonWSType(0) !== E && this.error();
          }
        },
        t || (() => this.Raw(null, !0))
      );
    }
    const Wi = {
      layer() {
        this.skipSC();
        const e = this.createList(), t = Hn.call(this, this.Layer);
        return (t.type !== "Raw" || t.value !== "") && e.push(t), e;
      },
      supports() {
        this.skipSC();
        const e = this.createList(), t = Hn.call(
          this,
          this.Declaration,
          () => Hn.call(this, () => this.Condition("supports"))
        );
        return (t.type !== "Raw" || t.value !== "") && e.push(t), e;
      }
    }, um = {
      parse: {
        prelude() {
          const e = this.createList();
          switch (this.tokenType) {
            case Te:
              e.push(this.String());
              break;
            case te:
            case $:
              e.push(this.Url());
              break;
            default:
              this.error("String or url() is expected");
          }
          return this.skipSC(), this.tokenType === y && this.cmpStr(this.tokenStart, this.tokenEnd, "layer") ? e.push(this.Identifier()) : this.tokenType === $ && this.cmpStr(this.tokenStart, this.tokenEnd, "layer(") && e.push(this.Function(null, Wi)), this.skipSC(), this.tokenType === $ && this.cmpStr(this.tokenStart, this.tokenEnd, "supports(") && e.push(this.Function(null, Wi)), (this.lookupNonWSType(0) === y || this.lookupNonWSType(0) === M) && e.push(this.MediaQueryList()), e;
        },
        block: null
      }
    }, hm = {
      parse: {
        prelude() {
          return this.createSingleNodeList(
            this.LayerList()
          );
        },
        block() {
          return this.Block(!1);
        }
      }
    }, fm = {
      parse: {
        prelude() {
          return this.createSingleNodeList(
            this.MediaQueryList()
          );
        },
        block(e = !1) {
          return this.Block(e);
        }
      }
    }, pm = {
      parse: {
        prelude() {
          return this.createSingleNodeList(
            this.SelectorList()
          );
        },
        block() {
          return this.Block(!0);
        }
      }
    }, dm = {
      parse: {
        prelude() {
          return this.createSingleNodeList(
            this.SelectorList()
          );
        },
        block() {
          return this.Block(!0);
        }
      }
    }, mm = {
      parse: {
        prelude() {
          return this.createSingleNodeList(
            this.Scope()
          );
        },
        block(e = !1) {
          return this.Block(e);
        }
      }
    }, gm = {
      parse: {
        prelude: null,
        block(e = !1) {
          return this.Block(e);
        }
      }
    }, bm = {
      parse: {
        prelude() {
          return this.createSingleNodeList(
            this.Condition("supports")
          );
        },
        block(e = !1) {
          return this.Block(e);
        }
      }
    }, ym = {
      container: lm,
      "font-face": cm,
      import: um,
      layer: hm,
      media: fm,
      nest: pm,
      page: dm,
      scope: mm,
      "starting-style": gm,
      supports: bm
    };
    function km() {
      const e = this.createList();
      this.skipSC();
      e: for (; !this.eof; ) {
        switch (this.tokenType) {
          case y:
            e.push(this.Identifier());
            break;
          case Te:
            e.push(this.String());
            break;
          case ce:
            e.push(this.Operator());
            break;
          case E:
            break e;
          default:
            this.error("Identifier, string or comma is expected");
        }
        this.skipSC();
      }
      return e;
    }
    const qe = {
      parse() {
        return this.createSingleNodeList(
          this.SelectorList()
        );
      }
    }, Un = {
      parse() {
        return this.createSingleNodeList(
          this.Selector()
        );
      }
    }, xm = {
      parse() {
        return this.createSingleNodeList(
          this.Identifier()
        );
      }
    }, wm = {
      parse: km
    }, Ht = {
      parse() {
        return this.createSingleNodeList(
          this.Nth()
        );
      }
    }, vm = {
      dir: xm,
      has: qe,
      lang: wm,
      matches: qe,
      is: qe,
      "-moz-any": qe,
      "-webkit-any": qe,
      where: qe,
      not: qe,
      "nth-child": Ht,
      "nth-last-child": Ht,
      "nth-last-of-type": Ht,
      "nth-of-type": Ht,
      slotted: Un,
      host: Un,
      "host-context": Un
    }, Sm = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
      __proto__: null,
      AnPlusB: Ko,
      Atrule: Yo,
      AtrulePrelude: Qo,
      AttributeSelector: Zo,
      Block: es,
      Brackets: ts,
      CDC: ns,
      CDO: rs,
      ClassSelector: is,
      Combinator: os,
      Comment: ss,
      Condition: as,
      Declaration: cs,
      DeclarationList: us,
      Dimension: hs,
      Feature: fs,
      FeatureFunction: ps,
      FeatureRange: ds,
      Function: ms,
      GeneralEnclosed: gs,
      Hash: bs,
      IdSelector: ks,
      Identifier: ys,
      Layer: xs,
      LayerList: ws,
      MediaQuery: vs,
      MediaQueryList: Ss,
      NestingSelector: Cs,
      Nth: Ts,
      Number: As,
      Operator: Os,
      Parentheses: Es,
      Percentage: Ls,
      PseudoClassSelector: $s,
      PseudoElementSelector: _s,
      Ratio: Ps,
      Raw: zs,
      Rule: Is,
      Scope: Rs,
      Selector: Ms,
      SelectorList: Ns,
      String: Fs,
      StyleSheet: Bs,
      SupportsDeclaration: Ws,
      TypeSelector: Hs,
      UnicodeRange: Gs,
      Url: Ks,
      Value: Ys,
      WhiteSpace: Qs
    }, Symbol.toStringTag, { value: "Module" })), Cm = {
      parseContext: {
        default: "StyleSheet",
        stylesheet: "StyleSheet",
        atrule: "Atrule",
        atrulePrelude(e) {
          return this.AtrulePrelude(e.atrule ? String(e.atrule) : null);
        },
        mediaQueryList: "MediaQueryList",
        mediaQuery: "MediaQuery",
        condition(e) {
          return this.Condition(e.kind);
        },
        rule: "Rule",
        selectorList: "SelectorList",
        selector: "Selector",
        block() {
          return this.Block(!0);
        },
        declarationList: "DeclarationList",
        declaration: "Declaration",
        value: "Value"
      },
      features: {
        supports: {
          selector() {
            return this.Selector();
          }
        },
        container: {
          style() {
            return this.Declaration();
          }
        }
      },
      scope: sm,
      atrule: ym,
      pseudo: vm,
      node: Sm
    }, Tm = {
      node: Xs
    }, Am = Qc(_(_(_({}, jd), Cm), Tm));
    function un(e) {
      const t = {};
      for (const n of Object.keys(e)) {
        let r = e[n];
        r && (Array.isArray(r) || r instanceof q ? r = r.map(un) : r.constructor === Object && (r = un(r))), t[n] = r;
      }
      return t;
    }
    const {
      tokenize: Kg,
      parse: Om,
      generate: Em,
      lexer: Yg,
      createLexer: Qg,
      walk: Fe,
      find: Xg,
      findLast: Zg,
      findAll: Jg,
      toPlainObject: eb,
      fromPlainObject: tb,
      fork: nb
    } = Am;
    let Lm = "useandom-26T198340PX75pxJACKVERYMINDBUSHWOLF_GQZbfghjklqvwyzrict", ke = (e = 21) => {
      let t = "", n = e;
      for (; n--; )
        t += Lm[Math.random() * 64 | 0];
      return t;
    };
    const oe = ke();
    function Ir(e) {
      return !!(e && e.type === "Function" && e.name === "anchor");
    }
    function Be(e) {
      return Om(e, {
        parseAtrulePrelude: !1,
        parseCustomProperty: !0
      });
    }
    function le(e) {
      return Em(e, {
        // Default `safe` adds extra (potentially breaking) spaces for compatibility
        // with old browsers.
        mode: "spec"
      });
    }
    function $m(e) {
      return e.type === "Declaration";
    }
    function _m(e) {
      return e.toArray().reduce(
        (t, n) => n.type === "Operator" && n.value === "," ? (t.push([]), t) : (n.type === "Identifier" && t[t.length - 1].push(n), t),
        [[]]
      );
    }
    function ar(e) {
      return e ? e.children.map((t) => {
        var i;
        let n;
        ((i = t.children.last) == null ? void 0 : i.type) === "PseudoElementSelector" && (t = un(t), n = le(t.children.last), t.children.pop());
        const r = le(t);
        return {
          selector: r + (n != null ? n : ""),
          elementPart: r,
          pseudoElementPart: n
        };
      }).toArray() : [];
    }
    const lr = {
      "position-anchor": `--position-anchor-${oe}`,
      "anchor-scope": `--anchor-scope-${oe}`,
      "anchor-name": `--anchor-name-${oe}`,
      left: `--left-${oe}`,
      right: `--right-${oe}`,
      top: `--top-${oe}`,
      bottom: `--bottom-${oe}`,
      "inset-block-start": `--inset-block-start-${oe}`,
      "inset-block-end": `--inset-block-end-${oe}`,
      "inset-inline-start": `--inset-inline-start-${oe}`,
      "inset-inline-end": `--inset-inline-end-${oe}`,
      "inset-block": `--inset-block-${oe}`,
      "inset-inline": `--inset-inline-${oe}`,
      inset: `--inset-${oe}`
    };
    function Pm(e, t) {
      return $m(e) && lr[e.property] && t ? (t.children.appendData(Z(_({}, e), {
        property: lr[e.property]
      })), { updated: !0 }) : {};
    }
    function zm(e) {
      for (const t of e) {
        let n = !1;
        const r = Be(t.css);
        Fe(r, {
          visit: "Declaration",
          enter(i) {
            var c;
            const o = (c = this.rule) == null ? void 0 : c.block, { updated: s } = Pm(i, o);
            s && (n = !0);
          }
        }), n && (t.css = le(r), t.changed = !0);
      }
      return e.some((t) => t.changed === !0);
    }
    var Js = /* @__PURE__ */ ((e) => (e.All = "all", e.None = "none", e))(Js || {});
    function Ce(e, t) {
      var r;
      return t = (r = lr[t]) != null ? r : t, (e instanceof HTMLElement ? getComputedStyle(e) : e.computedStyle).getPropertyValue(t).trim();
    }
    function gt(e, t, n) {
      return Ce(e, t) === n;
    }
    function Im(e, { selector: t, pseudoElementPart: n }) {
      const r = getComputedStyle(e, n), i = document.createElement("div"), o = document.createElement("style");
      i.id = `fake-pseudo-element-${ke()}`;
      for (const c of Array.from(r)) {
        const l = r.getPropertyValue(c);
        i.style.setProperty(c, l);
      }
      o.textContent += `#${i.id}${n} { content: ${r.content}; }`, o.textContent += `${t} { display: none !important; }`, document.head.append(o);
      const s = n === "::before" ? "afterbegin" : "beforeend";
      return e.insertAdjacentElement(s, i), { fakePseudoElement: i, sheet: o, computedStyle: r };
    }
    function Rm(e) {
      let t = e;
      for (; t; ) {
        if (gt(t, "overflow", "scroll"))
          return t;
        t = t.parentElement;
      }
      return t;
    }
    function Mm(e) {
      let t = Rm(e);
      return t === document.documentElement && (t = null), t != null ? t : { scrollTop: 0, scrollLeft: 0 };
    }
    function Nm(e) {
      const { elementPart: t, pseudoElementPart: n } = e, r = [];
      if (n && !(n === "::before" || n === "::after")) return r;
      const s = Array.from(
        document.querySelectorAll(t)
      );
      if (!n)
        return r.push(...s), r;
      for (const c of s) {
        const { fakePseudoElement: l, sheet: a, computedStyle: u } = Im(
          c,
          e
        ), h = l.getBoundingClientRect(), { scrollY: d, scrollX: m } = globalThis, w = Mm(c);
        r.push({
          fakePseudoElement: l,
          computedStyle: u,
          removeFakePseudoElement() {
            l.remove(), a.remove();
          },
          // For https://floating-ui.com/docs/autoupdate#ancestorscroll to work on
          // `VirtualElement`s.
          contextElement: c,
          // https://floating-ui.com/docs/virtual-elements
          getBoundingClientRect() {
            const { scrollY: k, scrollX: C } = globalThis, { scrollTop: b, scrollLeft: x } = w;
            return DOMRect.fromRect({
              y: h.y + (d - k) + (w.scrollTop - b),
              x: h.x + (m - C) + (w.scrollLeft - x),
              width: h.width,
              height: h.height
            });
          }
        });
      }
      return r;
    }
    function Dm(e, t) {
      const n = Ce(e, "anchor-name");
      return t ? n.split(",").map((r) => r.trim()).includes(t) : !n;
    }
    function jm(e, t) {
      const n = Ce(e, "anchor-scope");
      return n === t || n === "all";
    }
    const Hi = "InvalidMimeType";
    function Fm(e) {
      return !!((e.type === "text/css" || e.rel === "stylesheet") && e.href);
    }
    function Bm(e) {
      const t = new URL(e.href, document.baseURI);
      if (Fm(e) && t.origin === location.origin)
        return t;
    }
    function Wm(e) {
      return H(this, null, function* () {
        return (yield Promise.all(
          e.map((n) => H(this, null, function* () {
            var r;
            if (!n.url)
              return n;
            if ((r = n.el) != null && r.disabled)
              return null;
            try {
              const i = yield fetch(n.url.toString()), o = i.headers.get("content-type");
              if (!(o != null && o.startsWith("text/css"))) {
                const c = new Error(
                  `Error loading ${n.url}: expected content-type "text/css", got "${o}".`
                );
                throw c.name = Hi, c;
              }
              const s = yield i.text();
              return Z(_({}, n), { css: s });
            } catch (i) {
              if (i instanceof Error && i.name === Hi)
                return console.warn(i), null;
              throw i;
            }
          }))
        )).filter((n) => n !== null);
      });
    }
    const Ui = '[style*="anchor"]';
    function Hm(e) {
      const t = e ? e.filter(
        (r) => r instanceof HTMLElement && r.matches(Ui)
      ) : Array.from(
        document.querySelectorAll(Ui)
      ), n = [];
      return t.filter((r) => r instanceof HTMLElement).forEach((r) => {
        const i = ke(12), o = "data-has-inline-styles";
        r.setAttribute(o, i);
        const s = r.getAttribute("style"), c = `[${o}="${i}"] { ${s} }`;
        n.push({ el: r, css: c });
      }), n;
    }
    function Um(e, t) {
      return H(this, null, function* () {
        const n = e != null ? e : Array.from(document.querySelectorAll("link, style")), r = [];
        n.filter((s) => s instanceof HTMLElement).forEach((s) => {
          if (s.tagName.toLowerCase() === "link") {
            const c = Bm(s);
            c && r.push({ el: s, url: c });
          }
          s.tagName.toLowerCase() === "style" && r.push({ el: s, css: s.innerHTML });
        });
        const i = t ? e != null ? e : [] : void 0, o = Hm(i);
        return yield Wm([...r, ...o]);
      });
    }
    const ea = [
      "left",
      "right",
      "top",
      "bottom",
      "inset-block-start",
      "inset-block-end",
      "inset-inline-start",
      "inset-inline-end",
      "inset-block",
      "inset-inline",
      "inset"
    ];
    function zt(e) {
      return ea.includes(e);
    }
    const ta = [
      "margin-block-start",
      "margin-block-end",
      "margin-block",
      "margin-inline-start",
      "margin-inline-end",
      "margin-inline",
      "margin-bottom",
      "margin-left",
      "margin-right",
      "margin-top",
      "margin"
    ];
    function qm(e) {
      return ta.includes(e);
    }
    const na = [
      "width",
      "height",
      "min-width",
      "min-height",
      "max-width",
      "max-height",
      "block-size",
      "inline-size",
      "min-block-size",
      "min-inline-size",
      "max-block-size",
      "max-inline-size"
    ];
    function vn(e) {
      return na.includes(e);
    }
    const ra = [
      "justify-self",
      "align-self",
      "place-self"
    ];
    function Gm(e) {
      return ra.includes(e);
    }
    const Vm = [
      ...ea,
      ...ta,
      ...na,
      ...ra,
      "position-anchor",
      "position-area"
    ], Km = [
      "top",
      "left",
      "right",
      "bottom",
      "start",
      "end",
      "self-start",
      "self-end",
      "center"
    ];
    function ia(e) {
      return Km.includes(e);
    }
    const Ym = [
      "width",
      "height",
      "block",
      "inline",
      "self-block",
      "self-inline"
    ];
    function Qm(e) {
      return Ym.includes(e);
    }
    const Xm = [
      "left",
      "center",
      "right",
      "span-left",
      "span-right",
      "x-start",
      "x-end",
      "span-x-start",
      "span-x-end",
      "x-self-start",
      "x-self-end",
      "span-x-self-start",
      "span-x-self-end",
      "span-all",
      "top",
      "bottom",
      "span-top",
      "span-bottom",
      "y-start",
      "y-end",
      "span-y-start",
      "span-y-end",
      "y-self-start",
      "y-self-end",
      "span-y-self-start",
      "span-y-self-end",
      "block-start",
      "block-end",
      "span-block-start",
      "span-block-end",
      "inline-start",
      "inline-end",
      "span-inline-start",
      "span-inline-end",
      "self-block-start",
      "self-block-end",
      "span-self-block-start",
      "span-self-block-end",
      "self-inline-start",
      "self-inline-end",
      "span-self-inline-start",
      "span-self-inline-end",
      "start",
      "end",
      "span-start",
      "span-end",
      "self-start",
      "self-end",
      "span-self-start",
      "span-self-end"
    ], Zm = [
      "normal",
      "most-width",
      "most-height",
      "most-block-size",
      "most-inline-size"
    ], Jm = [
      "flip-block",
      "flip-inline",
      "flip-start"
    ];
    function oa(e) {
      return Xm.includes(e);
    }
    function eg(e) {
      return e.type === "Declaration";
    }
    function tg(e) {
      return e.type === "Declaration" && e.property === "position-try-fallbacks";
    }
    function ng(e) {
      return e.type === "Declaration" && e.property === "position-try-order";
    }
    function rg(e) {
      return e.type === "Declaration" && e.property === "position-try";
    }
    function ig(e) {
      return e.type === "Atrule" && e.name === "position-try";
    }
    function og(e) {
      return Jm.includes(e);
    }
    function sg(e) {
      return Zm.includes(e);
    }
    function ag(e, t) {
      const n = document.querySelector(e);
      if (n) {
        let r = cg(n);
        return t.forEach((i) => {
          r = sa(r, i);
        }), r;
      }
    }
    function lg(e, t) {
      let n = e.declarations;
      return t.forEach((r) => {
        n = sa(n, r);
      }), n;
    }
    function cg(e) {
      const t = {};
      return Vm.forEach((n) => {
        const r = Ce(
          e,
          `--${n}-${oe}`
        );
        r && (t[n] = r);
      }), t;
    }
    const ug = {
      "flip-block": {
        top: "bottom",
        bottom: "top",
        "inset-block-start": "inset-block-end",
        "inset-block-end": "inset-block-start",
        "margin-top": "margin-bottom",
        "margin-bottom": "margin-top"
      },
      "flip-inline": {
        left: "right",
        right: "left",
        "inset-inline-start": "inset-inline-end",
        "inset-inline-end": "inset-inline-start",
        "margin-left": "margin-right",
        "margin-right": "margin-left"
      },
      "flip-start": {
        left: "top",
        right: "bottom",
        top: "left",
        bottom: "right",
        "inset-block-start": "inset-block-end",
        "inset-block-end": "inset-block-start",
        "inset-inline-start": "inset-inline-end",
        "inset-inline-end": "inset-inline-start",
        "inset-block": "inset-inline",
        "inset-inline": "inset-block"
      }
    }, hg = {
      "flip-block": {
        top: "bottom",
        bottom: "top",
        start: "end",
        end: "start",
        "self-end": "self-start",
        "self-start": "self-end"
      },
      "flip-inline": {
        left: "right",
        right: "left",
        start: "end",
        end: "start",
        "self-end": "self-start",
        "self-start": "self-end"
      },
      "flip-start": {
        top: "left",
        left: "top",
        right: "bottom",
        bottom: "right"
      }
    }, fg = {
      "flip-block": {
        top: "bottom",
        bottom: "top",
        start: "end",
        end: "start"
      },
      "flip-inline": {
        left: "right",
        right: "left",
        start: "end",
        end: "start"
      },
      "flip-start": {
        // TODO: Requires fuller logic
      }
    };
    function pg(e, t) {
      return ug[t][e] || e;
    }
    function dg(e, t) {
      return hg[t][e] || e;
    }
    function mg(e, t) {
      if (t === "flip-start")
        return e;
      {
        const n = fg[t];
        return e.split("-").map((r) => n[r] || r).join("-");
      }
    }
    function gg(e, t, n) {
      if (e === "margin") {
        const [r, i, o, s] = t.children.toArray();
        n === "flip-block" ? s ? t.children.fromArray([o, i, r, s]) : o && t.children.fromArray([o, i, r]) : n === "flip-inline" && s && t.children.fromArray([r, s, o, i]);
      } else if (e === "margin-block") {
        const [r, i] = t.children.toArray();
        n === "flip-block" && i && t.children.fromArray([i, r]);
      } else if (e === "margin-inline") {
        const [r, i] = t.children.toArray();
        n === "flip-inline" && i && t.children.fromArray([i, r]);
      }
    }
    const bg = (e, t) => {
      var i;
      return ((i = Be(`#id{${e}: ${t};}`).children.first) == null ? void 0 : i.block.children.first).value;
    };
    function sa(e, t) {
      const n = {};
      return Object.entries(e).forEach(([r, i]) => {
        const o = r, s = bg(o, i), c = pg(o, t);
        c !== o && ((n[o]) != null || (n[o] = "revert")), Fe(s, {
          visit: "Function",
          enter(a) {
            Ir(a) && a.children.forEach((u) => {
              Ot(u) && ia(u.name) && (u.name = dg(u.name, t));
            });
          }
        }), o === "position-area" && s.children.forEach((a) => {
          Ot(a) && oa(a.name) && (a.name = mg(a.name, t));
        }), o.startsWith("margin") && gg(o, s, t), n[c] = le(s);
      }), n;
    }
    function aa(e) {
      const t = _m(e), n = [];
      return t.forEach((r) => {
        const i = {
          atRules: [],
          tactics: [],
          positionAreas: []
        };
        r.forEach((o) => {
          og(o.name) ? i.tactics.push(o.name) : o.name.startsWith("--") ? i.atRules.push(o.name) : oa(o.name) && i.positionAreas.push(o.name);
        }), i.positionAreas.length ? n.push({
          positionArea: i.positionAreas[0],
          type: "position-area"
        }) : i.atRules.length && i.tactics.length ? n.push({
          tactics: i.tactics,
          atRule: i.atRules[0],
          type: "at-rule-with-try-tactic"
        }) : i.atRules.length ? n.push({
          atRule: i.atRules[0],
          type: "at-rule"
        }) : i.tactics.length && n.push({
          tactics: i.tactics,
          type: "try-tactic"
        });
      }), n;
    }
    function yg(e) {
      return tg(e) && e.value.children.first ? aa(e.value.children) : [];
    }
    function kg(e) {
      if (rg(e) && e.value.children.first) {
        const t = un(e);
        let n;
        const r = t.value.children.first.name;
        r && sg(r) && (n = r, t.value.children.shift());
        const i = aa(t.value.children);
        return { order: n, options: i };
      }
      return {};
    }
    function xg(e) {
      return ng(e) && e.value.children.first ? {
        order: e.value.children.first.name
      } : {};
    }
    function wg(e) {
      const { order: t, options: n } = kg(e);
      if (t || n)
        return { order: t, options: n };
      const { order: r } = xg(e), i = yg(e);
      return r || i ? { order: r, options: i } : {};
    }
    function vg(e) {
      return zt(e.property) || qm(e.property) || vn(e.property) || Gm(e.property) || ["position-anchor", "position-area"].includes(e.property);
    }
    function Sg(e) {
      var t, n;
      if (ig(e) && ((t = e.prelude) != null && t.value) && ((n = e.block) != null && n.children)) {
        const r = e.prelude.value, i = e.block.children.filter(
          (s) => eg(s) && vg(s)
        ), o = {
          uuid: `${r}-try-${ke(12)}`,
          declarations: Object.fromEntries(
            i.map((s) => [s.property, le(s.value)])
          )
        };
        return { name: r, tryBlock: o };
      }
      return {};
    }
    function Cg(e) {
      const t = {}, n = {}, r = {};
      for (const i of e) {
        const o = Be(i.css);
        Fe(o, {
          visit: "Atrule",
          enter(s) {
            const { name: c, tryBlock: l } = Sg(s);
            c && l && (t[c] = l);
          }
        });
      }
      for (const i of e) {
        let o = !1;
        const s = /* @__PURE__ */ new Set(), c = Be(i.css);
        Fe(c, {
          visit: "Declaration",
          enter(l) {
            var w;
            const a = (w = this.rule) == null ? void 0 : w.prelude, u = ar(a);
            if (!u.length) return;
            const { order: h, options: d } = wg(l), m = {};
            h && (m.order = h), u.forEach(({ selector: k }) => {
              var C;
              d == null || d.forEach((x) => {
                var P;
                let T;
                if (x.type === "at-rule")
                  T = x.atRule;
                else if (x.type === "try-tactic") {
                  T = `${k}-${x.tactics.join("-")}`;
                  const O = ag(
                    k,
                    x.tactics
                  );
                  O && (t[T] = {
                    uuid: `${k}-${x.tactics.join("-")}-try-${ke(12)}`,
                    declarations: O
                  });
                } else if (x.type === "at-rule-with-try-tactic") {
                  T = `${k}-${x.atRule}-${x.tactics.join("-")}`;
                  const O = t[x.atRule], f = lg(
                    O,
                    x.tactics
                  );
                  f && (t[T] = {
                    uuid: `${k}-${x.atRule}-${x.tactics.join("-")}-try-${ke(12)}`,
                    declarations: f
                  });
                }
                if (T && t[T]) {
                  const O = `[data-anchor-polyfill="${t[T].uuid}"]`;
                  (n[O]) != null || (n[O] = []), n[O].push(k), s.has(T) || ((m.fallbacks) != null || (m.fallbacks = []), m.fallbacks.push(t[T]), s.add(T), (P = this.stylesheet) == null || P.children.prependData({
                    type: "Rule",
                    prelude: {
                      type: "Raw",
                      value: O
                    },
                    block: {
                      type: "Block",
                      children: new q().fromArray(
                        Object.entries(t[T].declarations).map(
                          ([f, p]) => ({
                            type: "Declaration",
                            important: !0,
                            property: f,
                            value: {
                              type: "Raw",
                              value: p
                            }
                          })
                        )
                      )
                    }
                  }), o = !0);
                }
              }), Object.keys(m).length > 0 && (r[k] ? (m.order && (r[k].order = m.order), m.fallbacks && (((C = r[k]).fallbacks) != null || (C.fallbacks = []), r[k].fallbacks.push(
                ...m.fallbacks
              ))) : r[k] = m);
            });
          }
        }), o && (i.css = le(c), i.changed = !0);
      }
      return { fallbackTargets: n, validPositions: r };
    }
    function Tg(e, t) {
      return !e || e === t ? !1 : la(e) ? e.document.contains(t) : e.contains(t);
    }
    function la(e) {
      return !!(e && e === e.window);
    }
    function Ag(e) {
      return gt(e, "position", "fixed");
    }
    function cr(e) {
      return !!(e && (Ag(e) || gt(e, "position", "absolute")));
    }
    function qi(e, t) {
      return e.compareDocumentPosition(t) & Node.DOCUMENT_POSITION_FOLLOWING;
    }
    function Og(e) {
      return H(this, null, function* () {
        return yield ee.getOffsetParent(e);
      });
    }
    function qn(e) {
      return H(this, null, function* () {
        if (!["absolute", "fixed"].includes(Ce(e, "position")))
          return yield Og(e);
        let t = e.parentElement;
        for (; t; ) {
          if (!gt(t, "position", "static") && gt(t, "display", "block"))
            return t;
          t = t.parentElement;
        }
        return window;
      });
    }
    function Eg(e, t, n, r) {
      return H(this, null, function* () {
        const i = yield qn(e), o = yield qn(n);
        if (!(Tg(o, e) || la(o)) || i === o && !(!cr(e) || qi(e, n)))
          return !1;
        if (i !== o) {
          let s;
          const c = [];
          for (s = i; s && s !== o && s !== window; )
            c.push(s), s = yield qn(s);
          const l = c[c.length - 1];
          if (l instanceof HTMLElement && !(!cr(l) || qi(l, n)))
            return !1;
        }
        {
          let s = e.parentElement;
          for (; s; ) {
            if (gt(s, "content-visibility", "hidden"))
              return !1;
            s = s.parentElement;
          }
        }
        return !(t && r && Gi(e, t, r) !== Gi(n, t, r));
      });
    }
    function Gi(e, t, n) {
      for (; !(e.matches(n) && jm(e, t)); ) {
        if (!e.parentElement)
          return null;
        e = e.parentElement;
      }
      return e;
    }
    function Lg(e, t, n, r) {
      return H(this, null, function* () {
        if (!(e instanceof HTMLElement && n.length && cr(e)))
          return null;
        const i = n.flatMap(Nm).filter((s) => Dm(s, t)), o = r.map((s) => s.selector).join(",") || null;
        for (let s = i.length - 1; s >= 0; s--) {
          const c = i[s], l = "fakePseudoElement" in c;
          if (yield Eg(
            l ? c.fakePseudoElement : c,
            t,
            e,
            o
          ))
            return l && c.removeFakePseudoElement(), c;
        }
        return null;
      });
    }
    function $g(e) {
      return e.type === "Declaration" && e.property === "anchor-name";
    }
    function _g(e) {
      return e.type === "Declaration" && e.property === "anchor-scope";
    }
    function ca(e) {
      return !!(e && e.type === "Function" && e.name === "anchor-size");
    }
    function Xt(e) {
      return !!(e && e.type === "Function" && e.name === "var");
    }
    function Ot(e) {
      return !!(e.type === "Identifier" && e.name);
    }
    function Pg(e) {
      return !!(e.type === "Percentage" && e.value);
    }
    function Vi(e, t) {
      let n, r, i, o = "", s = !1, c;
      const l = [];
      e.children.toArray().forEach((d) => {
        if (s) {
          o = `${o}${le(d)}`;
          return;
        }
        if (d.type === "Operator" && d.value === ",") {
          s = !0;
          return;
        }
        l.push(d);
      });
      let [a, u] = l;
      if (u || (u = a, a = void 0), a && (Ot(a) && a.name.startsWith("--") ? n = a.name : Xt(a) && a.children.first && (c = a.children.first.name)), u)
        if (Ir(e)) {
          if (Ot(u) && ia(u.name))
            r = u.name;
          else if (Pg(u)) {
            const d = Number(u.value);
            r = Number.isNaN(d) ? void 0 : d;
          }
        } else ca(e) && Ot(u) && Qm(u.name) && (i = u.name);
      const h = `--anchor-${ke(12)}`;
      return Object.assign(e, {
        type: "Raw",
        value: `var(${h})`,
        children: null
      }), Reflect.deleteProperty(e, "name"), {
        anchorName: n,
        anchorSide: r,
        anchorSize: i,
        fallbackValue: o || "0px",
        customPropName: c,
        uuid: h
      };
    }
    function Ki(e) {
      return e.value.children.map(
        ({ name: t }) => t
      );
    }
    let at = {}, je = {}, Ke = {}, Et = {}, Ve = {};
    function zg() {
      at = {}, je = {}, Ke = {}, Et = {}, Ve = {};
    }
    function Ig(e, t) {
      var n;
      if ((Ir(e) || ca(e)) && t) {
        if (t.property.startsWith("--")) {
          const r = le(t.value), i = Vi(e);
          return Et[i.uuid] = r, Ke[t.property] = [
            ...(n = Ke[t.property]) != null ? n : [],
            i
          ], { changed: !0 };
        }
        if (zt(t.property) || vn(t.property)) {
          const r = Vi(e);
          return { prop: t.property, data: r, changed: !0 };
        }
      }
      return {};
    }
    function Rg(e, t) {
      return H(this, null, function* () {
        let n = t.anchorName;
        const r = t.customPropName;
        if (e && !n) {
          const c = Ce(
            e,
            "position-anchor"
          );
          c ? n = c : r && (n = Ce(e, r));
        }
        const i = n ? at[n] || [] : [], o = n ? je[Js.All] || [] : [], s = n ? je[n] || [] : [];
        return yield Lg(
          e,
          n || null,
          i,
          [...o, ...s]
        );
      });
    }
    function Mg(e) {
      return H(this, null, function* () {
        var l, a, u, h, d, m, w;
        const t = {};
        zg();
        const { fallbackTargets: n, validPositions: r } = Cg(e);
        for (const k of e) {
          let C = !1;
          const b = Be(k.css);
          Fe(b, function(x) {
            var f, g, K;
            const T = (f = this.rule) == null ? void 0 : f.prelude, v = ar(T);
            if ($g(x) && v.length)
              for (const R of Ki(x))
                (at[R]) != null || (at[R] = []), at[R].push(...v);
            if (_g(x) && v.length)
              for (const R of Ki(x))
                (je[R]) != null || (je[R] = []), je[R].push(...v);
            const {
              prop: A,
              data: P,
              changed: O
            } = Ig(x, this.declaration);
            if (A && P && v.length)
              for (const { selector: R } of v)
                t[R] = Z(_({}, t[R]), {
                  [A]: [...(K = (g = t[R]) == null ? void 0 : g[A]) != null ? K : [], P]
                });
            O && (C = !0);
          }), C && (k.css = le(b), k.changed = !0);
        }
        const i = new Set(Object.keys(Ke)), o = {}, s = (k) => {
          var x, T, v, A, P;
          const C = [], b = new Set((T = (x = o[k]) == null ? void 0 : x.names) != null ? T : []);
          for (; b.size > 0; )
            for (const O of b)
              C.push(...(v = Ke[O]) != null ? v : []), b.delete(O), (P = (A = o[O]) == null ? void 0 : A.names) != null && P.length && o[O].names.forEach((f) => b.add(f));
          return C;
        };
        for (; i.size > 0; ) {
          const k = [];
          for (const C of e) {
            let b = !1;
            const x = Be(C.css);
            Fe(x, {
              visit: "Function",
              enter(T) {
                var O, f;
                const v = (O = this.rule) == null ? void 0 : O.prelude, A = this.declaration, P = A == null ? void 0 : A.property;
                if ((v == null ? void 0 : v.children.isEmpty) === !1 && Xt(T) && A && P && T.children.first && i.has(
                  T.children.first.name
                ) && // For now, we only want assignments to other CSS custom properties
                P.startsWith("--")) {
                  const p = T.children.first, S = (f = Ke[p.name]) != null ? f : [], g = s(p.name);
                  if (!(S.length || g.length))
                    return;
                  const K = `${p.name}-anchor-${ke(12)}`, R = le(A.value);
                  Et[K] = R, o[P] || (o[P] = { names: [], uuids: [] });
                  const be = o[P];
                  be.names.includes(p.name) || be.names.push(p.name), be.uuids.push(K), k.push(P), p.name = K, b = !0;
                }
              }
            }), b && (C.css = le(x), C.changed = !0);
          }
          i.clear(), k.forEach((C) => i.add(C));
        }
        for (const k of e) {
          let C = !1;
          const b = Be(k.css);
          Fe(b, {
            visit: "Function",
            enter(x) {
              var P, O, f, p, S, g, K;
              const T = (P = this.rule) == null ? void 0 : P.prelude, v = this.declaration, A = v == null ? void 0 : v.property;
              if ((T == null ? void 0 : T.children.isEmpty) === !1 && Xt(x) && v && A && x.children.first && // Now we only want assignments to inset/sizing properties
              (zt(A) || vn(A))) {
                const R = x.children.first, be = (O = Ke[R.name]) != null ? O : [], Y = s(R.name);
                if (!(be.length || Y.length))
                  return;
                const ze = `${A}-${ke(12)}`;
                if (Y.length) {
                  const Ze = /* @__PURE__ */ new Set([R.name]);
                  for (; Ze.size > 0; )
                    for (const Je of Ze) {
                      const ie = o[Je];
                      if ((f = ie == null ? void 0 : ie.names) != null && f.length && ((p = ie == null ? void 0 : ie.uuids) != null && p.length))
                        for (const et of ie.names)
                          for (const tt of ie.uuids)
                            Ve[tt] = Z(_({}, Ve[tt]), {
                              // - `key` (`propUuid`) is the property-specific
                              //   uuid to append to the new custom property name
                              // - `value` is the new property-specific custom
                              //   property value to use
                              [ze]: `${et}-${ze}`
                            });
                      Ze.delete(Je), (S = ie == null ? void 0 : ie.names) != null && S.length && ie.names.forEach((et) => Ze.add(et));
                    }
                }
                const Ue = ar(T);
                for (const Ze of [...be, ...Y]) {
                  const Je = _({}, Ze), ie = `--anchor-${ke(12)}-${A}`, et = Je.uuid;
                  Je.uuid = ie;
                  for (const { selector: tt } of Ue)
                    t[tt] = Z(_({}, t[tt]), {
                      [A]: [...(K = (g = t[tt]) == null ? void 0 : g[A]) != null ? K : [], Je]
                    });
                  Ve[et] = Z(_({}, Ve[et]), {
                    // - `key` (`propUuid`) is the property-specific
                    //   uuid to append to the new custom property name
                    // - `value` is the new property-specific custom
                    //   property value to use
                    [ze]: ie
                  });
                }
                R.name = `${R.name}-${ze}`, C = !0;
              }
            }
          }), C && (k.css = le(b), k.changed = !0);
        }
        if (Object.keys(Ve).length > 0)
          for (const k of e) {
            let C = !1;
            const b = Be(k.css);
            Fe(b, {
              visit: "Function",
              enter(x) {
                var T, v, A, P;
                if (Xt(x) && ((v = (T = x.children.first) == null ? void 0 : T.name) != null && v.startsWith(
                  "--"
                )) && ((P = (A = this.declaration) == null ? void 0 : A.property) != null && P.startsWith("--")) && this.block) {
                  const O = x.children.first, f = Ve[O.name];
                  if (f)
                    for (const [p, S] of Object.entries(f))
                      this.block.children.appendData({
                        type: "Declaration",
                        important: !1,
                        property: `${this.declaration.property}-${p}`,
                        value: {
                          type: "Raw",
                          value: le(this.declaration.value).replace(
                            `var(${O.name})`,
                            `var(${S})`
                          )
                        }
                      }), C = !0;
                  Et[O.name] && (this.declaration.value = {
                    type: "Raw",
                    value: Et[O.name]
                  }, C = !0);
                }
              }
            }), C && (k.css = le(b), k.changed = !0);
          }
        const c = /* @__PURE__ */ new Map();
        for (const [k, C] of Object.entries(t)) {
          let b;
          k.startsWith("[data-anchor-polyfill=") && ((l = n[k]) != null && l.length) ? b = document.querySelectorAll(n[k].join(",")) : b = document.querySelectorAll(k);
          for (const [x, T] of Object.entries(C))
            for (const v of T)
              for (const A of b) {
                const P = yield Rg(A, v), O = `--anchor-${ke(12)}`;
                c.set(A, Z(_({}, (a = c.get(A)) != null ? a : {}), {
                  [v.uuid]: O
                })), A.setAttribute(
                  "style",
                  `${v.uuid}: var(${O}); ${(u = A.getAttribute("style")) != null ? u : ""}`
                ), r[k] = Z(_({}, r[k]), {
                  declarations: Z(_({}, (h = r[k]) == null ? void 0 : h.declarations), {
                    [x]: [
                      ...(w = (m = (d = r[k]) == null ? void 0 : d.declarations) == null ? void 0 : m[x]) != null ? w : [],
                      Z(_({}, v), { anchorEl: P, targetEl: A, uuid: O })
                    ]
                  })
                });
              }
        }
        return { rules: r, inlineStyles: c, anchorScopes: je };
      });
    }
    const Ng = [
      "crossorigin",
      "href",
      "integrity",
      "referrerpolicy"
    ];
    function Yi(e, t, n = !1) {
      return H(this, null, function* () {
        const r = [];
        for (const { el: i, css: o, changed: s } of e) {
          const c = { el: i, css: o, changed: !1 };
          if (s) {
            if (i.tagName.toLowerCase() === "style")
              i.innerHTML = o;
            else if (i instanceof HTMLLinkElement) {
              const l = new Blob([o], { type: "text/css" }), a = URL.createObjectURL(l), u = document.createElement("link");
              for (const d of i.getAttributeNames())
                if (!d.startsWith("on") && !Ng.includes(d)) {
                  const m = i.getAttribute(d);
                  m !== null && u.setAttribute(d, m);
                }
              u.setAttribute("href", a);
              const h = new Promise((d) => {
                u.onload = d;
              });
              i.insertAdjacentElement("beforebegin", u), yield h, i.remove(), c.el = u;
            } else if (i.hasAttribute("data-has-inline-styles")) {
              const l = i.getAttribute("data-has-inline-styles");
              if (l) {
                const a = `[data-has-inline-styles="${l}"]{`;
                let h = o.slice(a.length, 0 - "}".length);
                const d = t == null ? void 0 : t.get(i);
                if (d)
                  for (const [m, w] of Object.entries(d))
                    h = `${m}: var(${w}); ${h}`;
                i.setAttribute("style", h);
              }
            }
          }
          n && i.hasAttribute("data-has-inline-styles") && i.removeAttribute("data-has-inline-styles"), r.push(c);
        }
        return r;
      });
    }
    const Dg = Z(_({}, ee), { _c: /* @__PURE__ */ new Map() }), ua = (e) => H(void 0, null, function* () {
      var n, r, i;
      let t = yield (n = ee.getOffsetParent) == null ? void 0 : n.call(ee, e);
      return (yield (r = ee.isElement) == null ? void 0 : r.call(ee, t)) || (t = (yield (i = ee.getDocumentElement) == null ? void 0 : i.call(ee, e)) || window.document.documentElement), t;
    }), jg = (e, t) => {
      let n;
      switch (e) {
        case "start":
        case "self-start":
          n = 0;
          break;
        case "end":
        case "self-end":
          n = 100;
          break;
        default:
          typeof e == "number" && !Number.isNaN(e) && (n = e);
      }
      if (n !== void 0)
        return t ? 100 - n : n;
    }, Fg = (e, t) => {
      let n;
      switch (e) {
        case "block":
        case "self-block":
          n = t ? "width" : "height";
          break;
        case "inline":
        case "self-inline":
          n = t ? "height" : "width";
          break;
      }
      return n;
    }, Qi = (e) => {
      switch (e) {
        case "top":
        case "bottom":
          return "y";
        case "left":
        case "right":
          return "x";
      }
      return null;
    }, Bg = (e) => {
      switch (e) {
        case "x":
          return "width";
        case "y":
          return "height";
      }
      return null;
    }, Xi = (e) => Ce(e, "display") === "inline", Zi = (e, t) => (t === "x" ? ["border-left-width", "border-right-width"] : ["border-top-width", "border-bottom-width"]).reduce(
      (r, i) => r + parseInt(Ce(e, i), 10),
      0
    ) || 0, Ut = (e, t) => parseInt(Ce(e, `margin-${t}`), 10) || 0, Wg = (e) => ({
      top: Ut(e, "top"),
      right: Ut(e, "right"),
      bottom: Ut(e, "bottom"),
      left: Ut(e, "left")
    }), Ji = (s) => H(void 0, [s], function* ({
      targetEl: e,
      targetProperty: t,
      anchorRect: n,
      anchorSide: r,
      anchorSize: i,
      fallback: o
    }) {
      var c;
      if (!((i || r !== void 0) && e && n))
        return o;
      if (i) {
        if (!vn(t))
          return o;
        let l;
        switch (i) {
          case "width":
          case "height":
            l = i;
            break;
          default: {
            let a = !1;
            const u = Ce(e, "writing-mode");
            a = u.startsWith("vertical-") || u.startsWith("sideways-"), l = Fg(i, a);
          }
        }
        return l ? `${n[l]}px` : o;
      }
      if (r !== void 0) {
        let l, a;
        const u = Qi(t);
        if (!(zt(t) && u && (!zt(r) || u === Qi(r))))
          return o;
        switch (r) {
          case "left":
            l = 0;
            break;
          case "right":
            l = 100;
            break;
          case "top":
            l = 0;
            break;
          case "bottom":
            l = 100;
            break;
          case "center":
            l = 50;
            break;
          default:
            if (e) {
              const m = (yield (c = ee.isRTL) == null ? void 0 : c.call(ee, e)) || !1;
              l = jg(r, m);
            }
        }
        const h = typeof l == "number" && !Number.isNaN(l), d = Bg(u);
        if (h && d) {
          (t === "bottom" || t === "right") && (a = yield ua(e));
          let m = n[u] + n[d] * (l / 100);
          switch (t) {
            case "bottom": {
              if (!a)
                break;
              let w = a.clientHeight;
              if (w === 0 && Xi(a)) {
                const k = Zi(a, u);
                w = a.offsetHeight - k;
              }
              m = w - m;
              break;
            }
            case "right": {
              if (!a)
                break;
              let w = a.clientWidth;
              if (w === 0 && Xi(a)) {
                const k = Zi(a, u);
                w = a.offsetWidth - k;
              }
              m = w - m;
              break;
            }
          }
          return `${m}px`;
        }
      }
      return o;
    });
    function Hg(e, t = !1) {
      return H(this, null, function* () {
        const n = document.documentElement;
        for (const [r, i] of Object.entries(e))
          for (const o of i) {
            const s = o.anchorEl, c = o.targetEl;
            if (s && c)
              lo(
                s,
                c,
                () => H(this, null, function* () {
                  const l = yield ee.getElementRects({
                    reference: s,
                    floating: c,
                    strategy: "absolute"
                  }), a = yield Ji({
                    targetEl: c,
                    targetProperty: r,
                    anchorRect: l.reference,
                    anchorSide: o.anchorSide,
                    anchorSize: o.anchorSize,
                    fallback: o.fallbackValue
                  });
                  n.style.setProperty(o.uuid, a);
                }),
                { animationFrame: t }
              );
            else {
              const l = yield Ji({
                targetProperty: r,
                anchorSide: o.anchorSide,
                anchorSize: o.anchorSize,
                fallback: o.fallbackValue
              });
              n.style.setProperty(o.uuid, l);
            }
          }
      });
    }
    function eo(e, t) {
      return H(this, null, function* () {
        const n = yield ee.getElementRects({
          reference: e,
          floating: e,
          strategy: "absolute"
        });
        return yield Ma(
          {
            x: e.offsetLeft,
            y: e.offsetTop,
            platform: Dg,
            rects: n,
            elements: { floating: e },
            strategy: "absolute"
          },
          {
            boundary: t,
            rootBoundary: "document",
            padding: Wg(e)
          }
        );
      });
    }
    function Ug(e, t, n = !1) {
      return H(this, null, function* () {
        if (!t.length)
          return;
        const r = document.querySelectorAll(e);
        for (const i of r) {
          let o = !1;
          const s = yield ua(i);
          lo(
            // We're just checking whether the target element overflows, so we don't
            // care about the position of the anchor element in this case. Passing in
            // an empty object instead of a reference element avoids unnecessarily
            // watching for irrelevant changes.
            {},
            i,
            () => H(this, null, function* () {
              if (o)
                return;
              o = !0, i.removeAttribute("data-anchor-polyfill");
              const c = yield eo(i, s);
              if (Object.values(c).every((l) => l <= 0)) {
                i.removeAttribute("data-anchor-polyfill-last-successful"), o = !1;
                return;
              }
              for (const [l, { uuid: a }] of t.entries()) {
                i.setAttribute("data-anchor-polyfill", a);
                const u = yield eo(i, s);
                if (Object.values(u).every((h) => h <= 0)) {
                  i.setAttribute("data-anchor-polyfill-last-successful", a), o = !1;
                  break;
                }
                if (l === t.length - 1) {
                  const h = i.getAttribute(
                    "data-anchor-polyfill-last-successful"
                  );
                  h ? i.setAttribute("data-anchor-polyfill", h) : i.removeAttribute("data-anchor-polyfill"), o = !1;
                  break;
                }
              }
            }),
            { animationFrame: n, layoutShift: !1 }
          );
        }
      });
    }
    function qg(e, t = !1) {
      return H(this, null, function* () {
        var n, r;
        for (const i of Object.values(e))
          yield Hg((n = i.declarations) != null ? n : {}, t);
        for (const [i, o] of Object.entries(e))
          yield Ug(
            i,
            (r = o.fallbacks) != null ? r : [],
            t
          );
      });
    }
    function Gg(e = {}) {
      const t = typeof e == "boolean" ? { useAnimationFrame: e } : e, n = t.useAnimationFrame === void 0 ? !!window.UPDATE_ANCHOR_ON_ANIMATION_FRAME : t.useAnimationFrame;
      return Array.isArray(t.elements) || (t.elements = void 0), Object.assign(t, { useAnimationFrame: n });
    }
    function rb(e) {
      return H(this, null, function* () {
        const t = Gg(
          window.ANCHOR_POSITIONING_POLYFILL_OPTIONS
        );
        let n = yield Um(t.elements, t.excludeInlineStyles);
        (yield zm(n)) && (n = yield Yi(n));
        const { rules: i, inlineStyles: o } = yield Mg(n);
        return Object.values(i).length && (yield Yi(n, o, !0), yield qg(i, t.useAnimationFrame)), i;
      });
    }

    class Options{constructor(_0x4211be={}){this['delay']=0x1f4,this['minimum_characters']=0x2,this['suggestion_count']=0x6,this['item_template']=_0x1476a6=>{return '<div\x20style=\x22display:flex;\x22><div\x20class=\x22ga-autocomplete-icon\x20ga-autocomplete-location-icon\x22></div><div\x20style=\x22width:100%\x22>'+_0x1476a6['address']+'</div></div>';},this['mobile_item_template']=undefined,this['footer_template']=undefined,this['history_item_template']=undefined,this['mobile_history_item_template']=undefined,this['suggestion_template']='<div><b>{formatted_address_0}</b></div><div>{formatted_address_1}{formatted_address_1,,\x20}{formatted_address_2}{formatted_address_2,,\x20}{formatted_address_3}{formatted_address_4,,\x20}{formatted_address_4}{postcode,,\x20}{postcode}</div>',this['filter']=undefined,this['show_postcode']=![],this['list_style']=undefined,this['list_item_style']=undefined,this['mobile_list_item_style']=undefined,this['history_item_style']=undefined,this['mobile_history_item_style']=undefined,this['mobile_list_style']=undefined,this['enable_repositioning']=!![],this['highlight_search_text']=![],this['enable_history']=!![],this['full_screen_on_mobile']=!![],this['mobile_max_screen_width']=0x1f4,this['selected']=undefined,this['suggested']=undefined,this['selected_failed']=undefined,this['suggested_failed']=undefined,this['full_length']=![],Object['assign'](this,_0x4211be);}}

    class ModualBackButton{constructor(_0x4f4162,_0x494238){this['modal']=_0x4f4162,this['input']=_0x494238,this['button']=document['createElement']('BUTTON'),this['element']=this['button'],this['build']=()=>{const _0x235b87=document['createElement']('i');_0x235b87['className']='ga-autocomplete-mobile-controls-icon',_0x235b87['classList']['add']('ga-autocomplete-mobile-back-icon'),this['button']['insertAdjacentElement']('afterbegin',_0x235b87),this['button']['className']='ga-autocomplete-mobile-back-button',this['button']['classList']['add']('ga-autocomplete-mobile-button'),this['button']['addEventListener']('click',this['handleClick']);},this['handleClick']=_0x2dc5fa=>{this['modal']['close']();},this['build']();}['destroy'](){this['button']['removeEventListener']('click',this['handleClick']);}}

    class ClearButton{constructor(_0x35ad76){this['input']=_0x35ad76,this['button']=document['createElement']('BUTTON'),this['element']=this['button'],this['build']=()=>{this['element']['setAttribute']('disabled','');const _0x192fa1=document['createElement']('i');_0x192fa1['className']='ga-autocomplete-mobile-controls-icon',_0x192fa1['classList']['add']('ga-autocomplete-mobile-clear-icon'),this['button']['insertAdjacentElement']('afterbegin',_0x192fa1),this['button']['className']='ga-autocomplete-mobile-clear-button',this['button']['classList']['add']('ga-autocomplete-mobile-button'),this['input']['textbox']['addEventListener']('input',this['handleTextInput']),this['input']['textbox']['addEventListener']('focus',this['handleFocus']),this['button']['addEventListener']('click',this['handleClick']);},this['handleTextInput']=_0x393faf=>{this['setDisabled']();},this['handleFocus']=_0xa86e04=>{this['setDisabled']();},this['setDisabled']=()=>{this['input']['textbox']['value']?this['element']['removeAttribute']('disabled'):this['element']['setAttribute']('disabled','');},this['handleClick']=_0x3b0d9c=>{this['input']['clear'](),this['input']['setFocus'](),this['setDisabled']();},this['build']();}['destroy'](){this['button']['removeEventListener']('click',this['handleClick']),this['input']['textbox']['removeEventListener']('input',this['handleTextInput']),this['input']['textbox']['removeEventListener']('focus',this['handleFocus']);}}

    class ModalControlsContainer{constructor(_0x2ba4b8,_0x2b8a19){this['container']=document['createElement']('DIV'),this['element']=this['container'],this['build']=()=>{this['container']['className']='ga-autocomplete-mobile-controls-container';const _0xa4c215=document['createElement']('DIV');_0xa4c215['className']='ga-autocomplete-mobile-controls-container-col-1',_0xa4c215['classList']['add']('ga-autocomplete-mobile-controls-container-col'),_0xa4c215['insertAdjacentElement']('afterbegin',this['backButton']['element']);const _0x3c60a0=document['createElement']('DIV');_0x3c60a0['className']='ga-autocomplete-mobile-controls-container-col-2',_0x3c60a0['classList']['add']('ga-autocomplete-mobile-controls-container-col'),_0x3c60a0['insertAdjacentElement']('afterbegin',this['input']['textbox']);const _0x1eb478=document['createElement']('DIV');_0x1eb478['className']='ga-autocomplete-mobile-controls-container-col-3',_0x1eb478['classList']['add']('ga-autocomplete-mobile-controls-container-col'),_0x1eb478['insertAdjacentElement']('afterbegin',this['clearButton']['element']),this['container']['insertAdjacentElement']('afterbegin',_0x1eb478),this['container']['insertAdjacentElement']('afterbegin',_0x3c60a0),this['container']['insertAdjacentElement']('afterbegin',_0xa4c215);},this['focus']=()=>{this['input']['setFocus']();},this['input']=_0x2b8a19['input'],this['backButton']=new ModualBackButton(_0x2ba4b8,this['input']),this['clearButton']=new ClearButton(this['input']),this['build']();}['destroy'](){this['backButton']['destroy'](),this['clearButton']['destroy'](),this['input']['destroy']();}}

    var __awaiter$4=undefined&&undefined['__awaiter']||function(_0x30ef42,_0x48686e,_0x59c96c,_0x4a2ea0){function _0x4a3d3d(_0x321bc7){return _0x321bc7 instanceof _0x59c96c?_0x321bc7:new _0x59c96c(function(_0x11d575){_0x11d575(_0x321bc7);});}return new(_0x59c96c||(_0x59c96c=Promise))(function(_0x8d0bc4,_0x5c272e){function _0x19b94e(_0x52840d){try{_0x101977(_0x4a2ea0['next'](_0x52840d));}catch(_0x144ed5){_0x5c272e(_0x144ed5);}}function _0xfddfa0(_0x4e34bd){try{_0x101977(_0x4a2ea0['throw'](_0x4e34bd));}catch(_0x5393a3){_0x5c272e(_0x5393a3);}}function _0x101977(_0x44ff3b){_0x44ff3b['done']?_0x8d0bc4(_0x44ff3b['value']):_0x4a3d3d(_0x44ff3b['value'])['then'](_0x19b94e,_0xfddfa0);}_0x101977((_0x4a2ea0=_0x4a2ea0['apply'](_0x30ef42,_0x48686e||[]))['next']());});};class ModalHistoryContainer extends ItemContainer{constructor(_0x4d8983,_0x3ddf4f,_0x2b8902,_0x45e133,_0x3e93f3){var _0xe0f13f,_0x19fa44;super(_0x4d8983,_0x45e133),this['storedAddress']=_0x3ddf4f,this['options']=_0x2b8902,this['modal']=_0x3e93f3,this['handleEnterKey']=_0x4b8531=>__awaiter$4(this,void 0x0,void 0x0,function*(){yield this['getAddress'](),_0x4b8531['preventDefault']();}),this['destroy']=()=>{this['container']['removeEventListener']('click',this['handleClick']),super['destroy']();},this['handleClick']=_0x12e95d=>__awaiter$4(this,void 0x0,void 0x0,function*(){yield this['getAddress']();}),this['getAddress']=()=>__awaiter$4(this,void 0x0,void 0x0,function*(){this['modal']['close'](),yield ItemContainer['getAddress'](this['input'],this['storedAddress']['suggestion'],this['storedAddress']['address']);});const _0x1c8c81=(_0x19fa44=(_0xe0f13f=_0x2b8902['mobile_history_item_template'])!==null&&_0xe0f13f!==void 0x0?_0xe0f13f:_0x2b8902['history_item_template'])!==null&&_0x19fa44!==void 0x0?_0x19fa44:_0x2b8902['item_template'];this['container']['innerHTML']=_0x1c8c81(_0x3ddf4f['suggestion']),this['container']['classList']['add']('ga-autocomplete-history-item'),this['container']['classList']['add']('ga-autocomplete-mobile-history-item'),this['container']['addEventListener']('click',this['handleClick']),_0x2b8902['mobile_history_item_style']&&this['container']['setAttribute']('style',_0x2b8902['mobile_history_item_style']);}}

    var __awaiter$3=undefined&&undefined['__awaiter']||function(_0x2730e5,_0x408dc3,_0x32050b,_0xe7e964){function _0x4b90fa(_0x1656a1){return _0x1656a1 instanceof _0x32050b?_0x1656a1:new _0x32050b(function(_0x1b4fa4){_0x1b4fa4(_0x1656a1);});}return new(_0x32050b||(_0x32050b=Promise))(function(_0x39666b,_0x2a7486){function _0x43cb53(_0x35a94f){try{_0x3c105b(_0xe7e964['next'](_0x35a94f));}catch(_0x5197fa){_0x2a7486(_0x5197fa);}}function _0x51dd30(_0x35a2a1){try{_0x3c105b(_0xe7e964['throw'](_0x35a2a1));}catch(_0x31152e){_0x2a7486(_0x31152e);}}function _0x3c105b(_0x424c31){_0x424c31['done']?_0x39666b(_0x424c31['value']):_0x4b90fa(_0x424c31['value'])['then'](_0x43cb53,_0x51dd30);}_0x3c105b((_0xe7e964=_0xe7e964['apply'](_0x2730e5,_0x408dc3||[]))['next']());});};class ModalItemContainer extends ItemContainer{constructor(_0x9ebdf1,_0x1147b0,_0x23bcbe,_0x59e36a,_0x526028,_0x533022){var _0x2267a2;super(_0x1147b0,_0x526028),this['client']=_0x9ebdf1,this['suggestion']=_0x23bcbe,this['options']=_0x59e36a,this['modal']=_0x533022,this['handleEnterKey']=_0x221432=>__awaiter$3(this,void 0x0,void 0x0,function*(){yield this['getAddress'](),_0x221432['preventDefault']();}),this['destroy']=()=>{this['container']['removeEventListener']('click',this['handleClick']),super['destroy']();},this['handleClick']=_0x474576=>__awaiter$3(this,void 0x0,void 0x0,function*(){yield this['getAddress']();}),this['getAddress']=()=>__awaiter$3(this,void 0x0,void 0x0,function*(){const _0x3e0eea=yield this['client']['get'](this['suggestion']['id']);if(!_0x3e0eea['isSuccess']){const _0x2ef513=_0x3e0eea['toFailed']();this['input']['dispatchSelectedFailed'](this['suggestion']['id'],_0x2ef513['status'],_0x2ef513['message']);return;}let _0x300483=_0x3e0eea['toSuccess']();this['options']['enable_history']&&Storage['save'](this['suggestion'],_0x300483['address']),this['modal']['close'](),ItemContainer['getAddress'](this['input'],this['suggestion'],_0x300483['address']);});const _0x3d1e97=(_0x2267a2=_0x59e36a['mobile_item_template'])!==null&&_0x2267a2!==void 0x0?_0x2267a2:_0x59e36a['item_template'];this['container']['innerHTML']=_0x3d1e97(_0x23bcbe),this['container']['classList']['add']('ga-autocomplete-mobile-list-item'),_0x59e36a['mobile_list_item_style']&&this['container']['setAttribute']('style',_0x59e36a['mobile_list_item_style']),this['container']['addEventListener']('click',this['handleClick']);}}

    var __awaiter$2=undefined&&undefined['__awaiter']||function(_0x46b5cf,_0x2c69d5,_0x2be767,_0x2882dc){function _0x2e59fb(_0xdb0e98){return _0xdb0e98 instanceof _0x2be767?_0xdb0e98:new _0x2be767(function(_0x1d99d9){_0x1d99d9(_0xdb0e98);});}return new(_0x2be767||(_0x2be767=Promise))(function(_0x5ee721,_0x1995e0){function _0x5baad7(_0xcb721d){try{_0x3d4c13(_0x2882dc['next'](_0xcb721d));}catch(_0x2bf256){_0x1995e0(_0x2bf256);}}function _0xcfa6fb(_0x526d2e){try{_0x3d4c13(_0x2882dc['throw'](_0x526d2e));}catch(_0x2809b9){_0x1995e0(_0x2809b9);}}function _0x3d4c13(_0x404467){_0x404467['done']?_0x5ee721(_0x404467['value']):_0x2e59fb(_0x404467['value'])['then'](_0x5baad7,_0xcfa6fb);}_0x3d4c13((_0x2882dc=_0x2882dc['apply'](_0x46b5cf,_0x2c69d5||[]))['next']());});};class ModalSuggester extends Suggester{constructor(_0x4e0d8c,_0xb51d98,_0x57d7cc,_0x209890){super(_0x4e0d8c,_0xb51d98,_0x57d7cc),this['modal']=_0x209890,this['getElements']=()=>__awaiter$2(this,void 0x0,void 0x0,function*(){const _0x23d7dc=this['input']['textbox']['value'];if(this['options']['enable_history']&&!this['hasMinimumCharacters'](_0x23d7dc)){const _0x4bd37b=Storage['list']();return _0x4bd37b['map']((_0x2b1db8,_0x585760)=>{return new ModalHistoryContainer(this['input'],_0x2b1db8,this['options'],_0x585760,this['modal'])['element'];});}if(!this['hasMinimumCharacters'](_0x23d7dc))return [];const _0x1f8ea2=yield this['getSuggestions'](_0x23d7dc);return _0x1f8ea2['map']((_0x215edc,_0x12a720)=>{return new ModalItemContainer(this['client'],this['input'],_0x215edc,this['options'],_0x12a720,this['modal'])['element'];});});}}

    var __awaiter$1=undefined&&undefined['__awaiter']||function(_0x19e651,_0x5ad7f2,_0x297694,_0xc4b2e3){function _0x1f3ea2(_0x2384cd){return _0x2384cd instanceof _0x297694?_0x2384cd:new _0x297694(function(_0x5a5e3e){_0x5a5e3e(_0x2384cd);});}return new(_0x297694||(_0x297694=Promise))(function(_0x45e9c5,_0x509fea){function _0x3451c6(_0x401695){try{_0x58d155(_0xc4b2e3['next'](_0x401695));}catch(_0x47e1c2){_0x509fea(_0x47e1c2);}}function _0x154b70(_0x3474e7){try{_0x58d155(_0xc4b2e3['throw'](_0x3474e7));}catch(_0x1373e9){_0x509fea(_0x1373e9);}}function _0x58d155(_0x3cac60){_0x3cac60['done']?_0x45e9c5(_0x3cac60['value']):_0x1f3ea2(_0x3cac60['value'])['then'](_0x3451c6,_0x154b70);}_0x58d155((_0xc4b2e3=_0xc4b2e3['apply'](_0x19e651,_0x5ad7f2||[]))['next']());});};class ModalInput extends Input{constructor(_0x2b155b,_0x2e64ca,_0x8f5987,_0x2c9988){const _0x55a3de=document['createElement']('INPUT');_0x55a3de['className']='ga-autocomplete-mobile-input',super(_0x2e64ca,_0x55a3de,_0x8f5987),this['options']=_0x2e64ca,this['list']=_0x8f5987,this['handleFocusOut']=_0x4e20a8=>{},this['handleKeyUp']=_0x4ad7bf=>__awaiter$1(this,void 0x0,void 0x0,function*(){yield Input['handleKeyUp'](_0x4ad7bf,this['suggester'],this['list']);}),this['handlePaste']=_0x47f0ca=>__awaiter$1(this,void 0x0,void 0x0,function*(){Input['handlePaste'](_0x47f0ca,this['suggester'],this['list']);}),this['handleDownKey']=_0x2fb72c=>__awaiter$1(this,void 0x0,void 0x0,function*(){yield Input['handleDownKey'](_0x2fb72c,this['list'],this['suggester']);}),this['handleKeyDownDefault']=_0x354f85=>__awaiter$1(this,void 0x0,void 0x0,function*(){Input['handleKeyDownDefault'](_0x354f85,this['list'],this['suggester']);}),this['populateList']=()=>__awaiter$1(this,void 0x0,void 0x0,function*(){yield Input['populateList'](this['suggester'],this['list']);}),this['handleFocus']=_0x4e42c5=>__awaiter$1(this,void 0x0,void 0x0,function*(){yield Input['handleFocus'](_0x4e42c5,this['list'],this['suggester'],this['repopulate']);}),this['suggester']=new ModalSuggester(_0x2b155b,this,_0x2e64ca,_0x2c9988),this['addEventHandlers']();}}

    class ModalList extends List{constructor(_0x30948c,_0x4d8b07,_0x47e8ad,_0x4922a4){super(_0x4d8b07,_0x47e8ad),this['options']=_0x4d8b07,this['instance']=_0x47e8ad,this['setFocus']=()=>{List['setFocus'](this['selectedIndex'],this,this['input']);},this['input']=new ModalInput(_0x30948c,_0x4d8b07,this,_0x4922a4),this['list']['classList']['add']('ga-autocomplete-mobile-list'),_0x4d8b07['mobile_list_style']&&this['list']['setAttribute']('style',_0x4d8b07['mobile_list_style']);}['setSelectedIndex'](_0x5dffaa){List['setSelectedIndex'](_0x5dffaa,this,this['input']);}['injectStyle'](){this['style']['appendChild'](document['createTextNode'](this['iconCss'](this['instance']))),this['style']['appendChild'](document['createTextNode'](this['listItemCss'](this['instance']))),this['list']['insertAdjacentElement']('beforebegin',this['style']);}['destroy'](){this['input']['destroy'](),super['destroy']();}}

    class Modal{constructor(_0x301398,_0x31a23f,_0x56b668,_0x1d905f){this['options']=_0x31a23f,this['textbox']=_0x1d905f,this['modal']=document['createElement']('DIV'),this['style']=document['createElement']('style'),this['element']=this['modal'],this['show']=()=>{this['modal']['style']['visibility']='visible',this['list']['show']();},this['hide']=()=>{this['modal']['style']['visibility']='hidden',this['list']['hide']();},this['modualCss']=_0x454ccc=>{if(_0x454ccc!==0x0)return '';const _0x2e4d19='\x0a\x20\x20\x20\x20\x20\x20\x20\x20/*\x20Modual\x20*/\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20display:\x20flex;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20flex-direction:\x20column;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20visibility:hidden;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20position:\x20fixed;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20z-index:\x20var(--ga-autocomplete-mobile-z-index,9999);\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20left:\x200;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20top:\x200;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x20100%;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20height:\x20100%;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20overflow:\x20clipped;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20background-color:\x20var(--ga-autocomplete-mobile-background-color,#fefefe);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-list{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin:\x200;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20height:\x20100%;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20overflow-x:\x20clipped;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20overflow-y:\x20scroll;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-bottom:var(--ga-autocomplete-mobile-margin-bottom,10px);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-top:var(--ga-autocomplete-mobile-margin-top,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-right:var(--ga-autocomplete-mobile-margin-right,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20margin-left:var(--ga-autocomplete-mobile-margin-left,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-list-item,\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-history-item\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding:0;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-top:\x20var(--ga-autocomplete-mobile-list-item-padding-top,5px);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-bottom:\x20var(--ga-autocomplete-mobile-list-item-padding-bottom,5px);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-right:\x20var(--ga-autocomplete-mobile-list-item-padding-right,0);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-left:\x20var(--ga-autocomplete-mobile-list-item-padding-left,0);\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-list-item\x20.ga-autocomplete-icon\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x203rem;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20text-align:\x20center;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding:\x200;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-top:\x20var(--ga-autocomplete-location-padding-top,4px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-bottom:\x20var(--ga-autocomplete-location-padding-bottom,2px);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-right:\x20var(--ga-autocomplete-location-padding-right,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-left:\x20var(--ga-autocomplete-location-padding-left,0);\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-size:\x201.2rem;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20line-height:\x201.2rem;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-history-item\x20.ga-autocomplete-icon\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20color:\x20grey;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}';return _0x2e4d19;},this['bodyCss']=_0x21b520=>{if(_0x21b520!==0x0)return '';const _0xc4cce2='\x0a\x20\x20\x20\x20\x20\x20\x20\x20/*\x20Body*/\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-body{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20overflow-y:\x20hidden;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}';return _0xc4cce2;},this['controlsContainerCss']=_0x19e3e8=>{if(_0x19e3e8!==0x0)return '';const _0x4219be='\x0a\x20\x20\x20\x20\x20\x20\x20\x20/*\x20Mobile\x20controls\x20container\x20*/\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-controls-container\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20border-bottom:\x201px\x20solid\x20#f1f1f1;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20display:\x20flex;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x20100%;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20justify-content:\x20space-between;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20align-items:\x20center;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20position:\x20sticky;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20top:0;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20left:\x200;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20background-color:\x20#fff;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-controls-container-col\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20align-self:\x20stretch;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20align-content:center;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-controls-container-col-1\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x203rem;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20text-align:\x20center;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-controls-container-col-2\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x20100%;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-controls-container-col-3\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20text-align:\x20center;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x203rem;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x0a\x20\x20\x20\x20.ga-autocomplete-mobile-controls-icon\x20\x0a\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20font-size:\x201.2rem;\x0a\x20\x20\x20\x20\x20\x20\x20\x20line-height:\x201.2rem;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20font-family:\x20\x27getaddress\x27;\x0a\x20\x20\x20\x20\x20\x20\x20\x20font-style:\x20normal;\x0a\x20\x20\x20\x20\x20\x20\x20\x20font-weight:\x20normal;\x0a\x20\x20\x20\x20\x20\x20\x20\x20speak:\x20never;\x0a\x20\x20\x20\x20\x20\x20\x20\x20display:\x20inline-block;\x0a\x20\x20\x20\x20\x20\x20\x20\x20text-decoration:\x20inherit;\x0a\x20\x20\x20\x20\x20\x20\x20\x20width:\x201em;\x0a\x20\x20\x20\x20\x20\x20\x20\x20margin-right:\x20.2em;\x0a\x20\x20\x20\x20\x20\x20\x20\x20text-align:\x20center;\x0a\x20\x20\x20\x20\x20\x20\x20\x20font-variant:\x20normal;\x0a\x20\x20\x20\x20\x20\x20\x20\x20text-transform:\x20none;\x0a\x20\x20\x20\x20\x20\x20\x20\x20margin-left:\x20.2em;\x0a\x20\x20\x20\x20\x20\x20\x20\x20-webkit-font-smoothing:\x20antialiased;\x0a\x20\x20\x20\x20\x20\x20\x20\x20-moz-osx-font-smoothing:\x20grayscale;\x0a\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20/*\x20mobile\x20buttons*/\x0a\x0a\x20\x20\x20\x20\x20.ga-autocomplete-mobile-button\x0a\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20background:\x20none;\x0a\x20\x20\x20\x20\x20\x20\x20\x20border:\x20none;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20.ga-autocomplete-mobile-back-icon:before\x20{\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20content:\x20\x27\x5ce801\x27;\x20\x0a\x20\x20\x20\x20}\x0a\x20\x20\x20\x0a\x20\x20\x20\x20/*\x20mobile\x20back\x20button*/\x0a\x20\x20\x20\x20.ga-autocomplete-mobile-clear-icon:before\x20{\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20content:\x20\x27\x5ce800\x27;\x20\x0a\x20\x20\x20\x20}\x0a\x20\x20\x20\x20.ga-autocomplete-mobile-clear-button\x0a\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20background:\x20none;\x0a\x20\x20\x20\x20\x20\x20\x20\x20border:\x20none;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x0a\x20\x20\x20\x20}\x20';return _0x4219be;},this['textboxCss']=_0x2ebd06=>{if(_0x2ebd06!==0x0)return '';const _0x3d00ac='\x0a\x20\x20\x20\x20\x20\x20\x20\x20/*\x20mobile\x20textbox\x20*/\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-input\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20width:\x20100%;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20font-size:\x201.2rem;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20line-height:\x201.2rem;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-top:\x2010px;\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20padding-bottom:\x2010px;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-input,\x0a\x20\x20\x20\x20\x20\x20\x20\x20.ga-autocomplete-mobile-input:focus\x0a\x20\x20\x20\x20\x20\x20\x20\x20{\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20border:\x200;\x20\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20outline:\x20none;\x0a\x20\x20\x20\x20\x20\x20\x20\x20}\x0a\x20\x20\x20\x20\x20\x20\x20\x20';return _0x3d00ac;},this['modal']['id']='ga-autocomplete-mobile-'+_0x56b668,this['modal']['className']='ga-autocomplete-mobile',_0x1d905f['addEventListener']('click',_0x1e9d6b=>{this['open'](_0x1d905f['value']);},![]),_0x1d905f['addEventListener']('keydown',_0x3bd2af=>{this['open'](_0x1d905f['value']);},![]),_0x1d905f['addEventListener']('paste',_0x5f0eba=>{this['open'](_0x1d905f['value']);},![]),this['list']=new ModalList(_0x301398,_0x31a23f,_0x56b668,this),this['controlsContainer']=new ModalControlsContainer(this,this['list']),this['modal']['insertBefore'](this['list']['element'],null),this['modal']['insertBefore'](this['controlsContainer']['element'],this['list']['element']),document['body']['insertBefore'](this['modal'],null),this['injectStyle'](_0x56b668),this['list']['injectStyle']();}['open'](_0x345dce){this['show'](),document['body']['classList']['add']('ga-autocomplete-mobile-body'),_0x345dce&&this['list']['input']['setValue'](_0x345dce),this['list']['input']['setFocus']();}['close'](){document['body']['classList']['remove']('ga-autocomplete-mobile-body'),this['hide']();const _0x75e63e=this['controlsContainer']['input']['value']();this['textbox']['value']=_0x75e63e,this['textbox']['focus']();}['injectStyle'](_0x1782e1){this['style']['appendChild'](document['createTextNode'](this['bodyCss'](_0x1782e1))),this['style']['appendChild'](document['createTextNode'](this['modualCss'](_0x1782e1))),this['style']['appendChild'](document['createTextNode'](this['controlsContainerCss'](_0x1782e1))),this['style']['appendChild'](document['createTextNode'](this['textboxCss'](_0x1782e1))),this['modal']['insertAdjacentElement']('beforebegin',this['style']);}}

    var __awaiter=undefined&&undefined['__awaiter']||function(_0x3762d0,_0x804514,_0x13cc16,_0x41213e){function _0x15343c(_0x65306){return _0x65306 instanceof _0x13cc16?_0x65306:new _0x13cc16(function(_0x2a269b){_0x2a269b(_0x65306);});}return new(_0x13cc16||(_0x13cc16=Promise))(function(_0x41a85a,_0x4cb193){function _0x3e924b(_0x5ecf97){try{_0x40f5db(_0x41213e['next'](_0x5ecf97));}catch(_0x174d62){_0x4cb193(_0x174d62);}}function _0x29e809(_0x236b6d){try{_0x40f5db(_0x41213e['throw'](_0x236b6d));}catch(_0x384f94){_0x4cb193(_0x384f94);}}function _0x40f5db(_0x12811e){_0x12811e['done']?_0x41a85a(_0x12811e['value']):_0x15343c(_0x12811e['value'])['then'](_0x3e924b,_0x29e809);}_0x40f5db((_0x41213e=_0x41213e['apply'](_0x3762d0,_0x804514||[]))['next']());});};class ListCounter{static['add'](_0x36714b){this['lists']['push'](_0x36714b);}}ListCounter['lists']=[];class ModalCounter{static['add'](_0x2fed12){this['modals']['push'](_0x2fed12);}}ModalCounter['modals']=[];const autocomplete=(_0x18a100,_0x21521d,_0x42d1bb={})=>{return new Promise((_0x5c1ce5,_0x3366e3)=>__awaiter(void 0x0,void 0x0,void 0x0,function*(){if(!_0x18a100){_0x3366e3('Not\x20found:\x20'+_0x18a100);return;}let _0x5625bc=document['getElementById'](_0x18a100);if(!_0x5625bc){_0x3366e3('Not\x20found:\x20'+_0x18a100);return;}try{const _0x427d46=new Options(_0x42d1bb);if(_0x427d46['full_screen_on_mobile']&&isTouchEnabled()&&screenWidth()<=_0x427d46['mobile_max_screen_width']){const _0x14ae4f=new Modal(_0x21521d,_0x427d46,ListCounter['lists']['length'],_0x5625bc);ModalCounter['add'](_0x14ae4f),_0x5c1ce5(_0x14ae4f['list']['element']);return;}yield rb();const _0x4efb29=new AnchoredList(_0x21521d,_0x427d46,_0x5625bc,ListCounter['lists']['length']);ListCounter['add'](_0x4efb29),_0x5c1ce5(_0x4efb29['element']);return;}catch(_0x21e48f){_0x3366e3(_0x21e48f);return;}}));},screenWidth=()=>{return window['innerWidth']>0x0?window['innerWidth']:screen['width'];},isTouchEnabled=()=>{return 'ontouchstart'in window||navigator['maxTouchPoints']>0x0;},destroy=()=>{for(const _0x207f2b of ListCounter['lists']){_0x207f2b['destroy']();}ListCounter['lists']=[];},clearHistory=()=>{Storage['clear']();},removeHistory=_0x43c631=>{Storage['remove'](_0x43c631);};

    exports.Options = Options;
    exports.autocomplete = autocomplete;
    exports.clearHistory = clearHistory;
    exports.destroy = destroy;
    exports.isTouchEnabled = isTouchEnabled;
    exports.removeHistory = removeHistory;
    exports.screenWidth = screenWidth;

    return exports;

})({});

/**
 * FingerprintJS v3.4.2 - Copyright (c) FingerprintJS, Inc, 2023 (https://fingerprint.com)
 * Licensed under the MIT (http://www.opensource.org/licenses/mit-license.php) license.
 *
 * This software contains code from open-source projects:
 * MurmurHash3 by Karan Lyons (https://github.com/karanlyons/murmurHash3.js)
 */

var FingerprintJS=function(e){"use strict";var n=function(){return n=Object.assign||function(e){for(var n,t=1,r=arguments.length;t<r;t++)for(var o in n=arguments[t])Object.prototype.hasOwnProperty.call(n,o)&&(e[o]=n[o]);return e},n.apply(this,arguments)};function t(e,n,t,r){return new(t||(t=Promise))((function(o,a){function i(e){try{u(r.next(e))}catch(n){a(n)}}function c(e){try{u(r.throw(e))}catch(n){a(n)}}function u(e){var n;e.done?o(e.value):(n=e.value,n instanceof t?n:new t((function(e){e(n)}))).then(i,c)}u((r=r.apply(e,n||[])).next())}))}function r(e,n){var t,r,o,a,i={label:0,sent:function(){if(1&o[0])throw o[1];return o[1]},trys:[],ops:[]};return a={next:c(0),throw:c(1),return:c(2)},"function"==typeof Symbol&&(a[Symbol.iterator]=function(){return this}),a;function c(c){return function(u){return function(c){if(t)throw new TypeError("Generator is already executing.");for(;a&&(a=0,c[0]&&(i=0)),i;)try{if(t=1,r&&(o=2&c[0]?r.return:c[0]?r.throw||((o=r.return)&&o.call(r),0):r.next)&&!(o=o.call(r,c[1])).done)return o;switch(r=0,o&&(c=[2&c[0],o.value]),c[0]){case 0:case 1:o=c;break;case 4:return i.label++,{value:c[1],done:!1};case 5:i.label++,r=c[1],c=[0];continue;case 7:c=i.ops.pop(),i.trys.pop();continue;default:if(!(o=i.trys,(o=o.length>0&&o[o.length-1])||6!==c[0]&&2!==c[0])){i=0;continue}if(3===c[0]&&(!o||c[1]>o[0]&&c[1]<o[3])){i.label=c[1];break}if(6===c[0]&&i.label<o[1]){i.label=o[1],o=c;break}if(o&&i.label<o[2]){i.label=o[2],i.ops.push(c);break}o[2]&&i.ops.pop(),i.trys.pop();continue}c=n.call(e,i)}catch(u){c=[6,u],r=0}finally{t=o=0}if(5&c[0])throw c[1];return{value:c[0]?c[1]:void 0,done:!0}}([c,u])}}}function o(e,n,t){if(t||2===arguments.length)for(var r,o=0,a=n.length;o<a;o++)!r&&o in n||(r||(r=Array.prototype.slice.call(n,0,o)),r[o]=n[o]);return e.concat(r||Array.prototype.slice.call(n))}var a="3.4.2";function i(e,n){return new Promise((function(t){return setTimeout(t,e,n)}))}function c(e){return!!e&&"function"==typeof e.then}function u(e,n){try{var t=e();c(t)?t.then((function(e){return n(!0,e)}),(function(e){return n(!1,e)})):n(!0,t)}catch(r){n(!1,r)}}function l(e,n,o){return void 0===o&&(o=16),t(this,void 0,void 0,(function(){var t,a,c,u;return r(this,(function(r){switch(r.label){case 0:t=Array(e.length),a=Date.now(),c=0,r.label=1;case 1:return c<e.length?(t[c]=n(e[c],c),(u=Date.now())>=a+o?(a=u,[4,i(0)]):[3,3]):[3,4];case 2:r.sent(),r.label=3;case 3:return++c,[3,1];case 4:return[2,t]}}))}))}function s(e){e.then(void 0,(function(){}))}function d(e,n){e=[e[0]>>>16,65535&e[0],e[1]>>>16,65535&e[1]],n=[n[0]>>>16,65535&n[0],n[1]>>>16,65535&n[1]];var t=[0,0,0,0];return t[3]+=e[3]+n[3],t[2]+=t[3]>>>16,t[3]&=65535,t[2]+=e[2]+n[2],t[1]+=t[2]>>>16,t[2]&=65535,t[1]+=e[1]+n[1],t[0]+=t[1]>>>16,t[1]&=65535,t[0]+=e[0]+n[0],t[0]&=65535,[t[0]<<16|t[1],t[2]<<16|t[3]]}function m(e,n){e=[e[0]>>>16,65535&e[0],e[1]>>>16,65535&e[1]],n=[n[0]>>>16,65535&n[0],n[1]>>>16,65535&n[1]];var t=[0,0,0,0];return t[3]+=e[3]*n[3],t[2]+=t[3]>>>16,t[3]&=65535,t[2]+=e[2]*n[3],t[1]+=t[2]>>>16,t[2]&=65535,t[2]+=e[3]*n[2],t[1]+=t[2]>>>16,t[2]&=65535,t[1]+=e[1]*n[3],t[0]+=t[1]>>>16,t[1]&=65535,t[1]+=e[2]*n[2],t[0]+=t[1]>>>16,t[1]&=65535,t[1]+=e[3]*n[1],t[0]+=t[1]>>>16,t[1]&=65535,t[0]+=e[0]*n[3]+e[1]*n[2]+e[2]*n[1]+e[3]*n[0],t[0]&=65535,[t[0]<<16|t[1],t[2]<<16|t[3]]}function f(e,n){return 32===(n%=64)?[e[1],e[0]]:n<32?[e[0]<<n|e[1]>>>32-n,e[1]<<n|e[0]>>>32-n]:(n-=32,[e[1]<<n|e[0]>>>32-n,e[0]<<n|e[1]>>>32-n])}function v(e,n){return 0===(n%=64)?e:n<32?[e[0]<<n|e[1]>>>32-n,e[1]<<n]:[e[1]<<n-32,0]}function h(e,n){return[e[0]^n[0],e[1]^n[1]]}function p(e){return e=h(e,[0,e[0]>>>1]),e=h(e=m(e,[4283543511,3981806797]),[0,e[0]>>>1]),e=h(e=m(e,[3301882366,444984403]),[0,e[0]>>>1])}function b(e,n){n=n||0;var t,r=(e=e||"").length%16,o=e.length-r,a=[0,n],i=[0,n],c=[0,0],u=[0,0],l=[2277735313,289559509],s=[1291169091,658871167];for(t=0;t<o;t+=16)c=[255&e.charCodeAt(t+4)|(255&e.charCodeAt(t+5))<<8|(255&e.charCodeAt(t+6))<<16|(255&e.charCodeAt(t+7))<<24,255&e.charCodeAt(t)|(255&e.charCodeAt(t+1))<<8|(255&e.charCodeAt(t+2))<<16|(255&e.charCodeAt(t+3))<<24],u=[255&e.charCodeAt(t+12)|(255&e.charCodeAt(t+13))<<8|(255&e.charCodeAt(t+14))<<16|(255&e.charCodeAt(t+15))<<24,255&e.charCodeAt(t+8)|(255&e.charCodeAt(t+9))<<8|(255&e.charCodeAt(t+10))<<16|(255&e.charCodeAt(t+11))<<24],c=f(c=m(c,l),31),a=d(a=f(a=h(a,c=m(c,s)),27),i),a=d(m(a,[0,5]),[0,1390208809]),u=f(u=m(u,s),33),i=d(i=f(i=h(i,u=m(u,l)),31),a),i=d(m(i,[0,5]),[0,944331445]);switch(c=[0,0],u=[0,0],r){case 15:u=h(u,v([0,e.charCodeAt(t+14)],48));case 14:u=h(u,v([0,e.charCodeAt(t+13)],40));case 13:u=h(u,v([0,e.charCodeAt(t+12)],32));case 12:u=h(u,v([0,e.charCodeAt(t+11)],24));case 11:u=h(u,v([0,e.charCodeAt(t+10)],16));case 10:u=h(u,v([0,e.charCodeAt(t+9)],8));case 9:u=m(u=h(u,[0,e.charCodeAt(t+8)]),s),i=h(i,u=m(u=f(u,33),l));case 8:c=h(c,v([0,e.charCodeAt(t+7)],56));case 7:c=h(c,v([0,e.charCodeAt(t+6)],48));case 6:c=h(c,v([0,e.charCodeAt(t+5)],40));case 5:c=h(c,v([0,e.charCodeAt(t+4)],32));case 4:c=h(c,v([0,e.charCodeAt(t+3)],24));case 3:c=h(c,v([0,e.charCodeAt(t+2)],16));case 2:c=h(c,v([0,e.charCodeAt(t+1)],8));case 1:c=m(c=h(c,[0,e.charCodeAt(t)]),l),a=h(a,c=m(c=f(c,31),s))}return a=d(a=h(a,[0,e.length]),i=h(i,[0,e.length])),i=d(i,a),a=d(a=p(a),i=p(i)),i=d(i,a),("00000000"+(a[0]>>>0).toString(16)).slice(-8)+("00000000"+(a[1]>>>0).toString(16)).slice(-8)+("00000000"+(i[0]>>>0).toString(16)).slice(-8)+("00000000"+(i[1]>>>0).toString(16)).slice(-8)}function y(e){return parseInt(e)}function g(e){return parseFloat(e)}function w(e,n){return"number"==typeof e&&isNaN(e)?n:e}function L(e){return e.reduce((function(e,n){return e+(n?1:0)}),0)}function k(e,n){if(void 0===n&&(n=1),Math.abs(n)>=1)return Math.round(e/n)*n;var t=1/n;return Math.round(e*t)/t}function V(e){return e&&"object"==typeof e&&"message"in e?e:{message:e}}function W(e){return"function"!=typeof e}function Z(e,n,o){var a=Object.keys(e).filter((function(e){return!function(e,n){for(var t=0,r=e.length;t<r;++t)if(e[t]===n)return!0;return!1}(o,e)})),i=l(a,(function(t){return function(e,n){var t=new Promise((function(t){var r=Date.now();u(e.bind(null,n),(function(){for(var e=[],n=0;n<arguments.length;n++)e[n]=arguments[n];var o=Date.now()-r;if(!e[0])return t((function(){return{error:V(e[1]),duration:o}}));var a=e[1];if(W(a))return t((function(){return{value:a,duration:o}}));t((function(){return new Promise((function(e){var n=Date.now();u(a,(function(){for(var t=[],r=0;r<arguments.length;r++)t[r]=arguments[r];var a=o+Date.now()-n;if(!t[0])return e({error:V(t[1]),duration:a});e({value:t[1],duration:a})}))}))}))}))}));return s(t),function(){return t.then((function(e){return e()}))}}(e[t],n)}));return s(i),function(){return t(this,void 0,void 0,(function(){var e,n,t,o;return r(this,(function(r){switch(r.label){case 0:return[4,i];case 1:return[4,l(r.sent(),(function(e){var n=e();return s(n),n}))];case 2:return e=r.sent(),[4,Promise.all(e)];case 3:for(n=r.sent(),t={},o=0;o<a.length;++o)t[a[o]]=n[o];return[2,t]}}))}))}}function S(){var e=window,n=navigator;return L(["MSCSSMatrix"in e,"msSetImmediate"in e,"msIndexedDB"in e,"msMaxTouchPoints"in n,"msPointerEnabled"in n])>=4}function C(){var e=window,n=navigator;return L(["msWriteProfilerMark"in e,"MSStream"in e,"msLaunchUri"in n,"msSaveBlob"in n])>=3&&!S()}function x(){var e=window,n=navigator;return L(["webkitPersistentStorage"in n,"webkitTemporaryStorage"in n,0===n.vendor.indexOf("Google"),"webkitResolveLocalFileSystemURL"in e,"BatteryManager"in e,"webkitMediaStream"in e,"webkitSpeechGrammar"in e])>=5}function F(){var e=window,n=navigator;return L(["ApplePayError"in e,"CSSPrimitiveValue"in e,"Counter"in e,0===n.vendor.indexOf("Apple"),"getStorageUpdates"in n,"WebKitMediaKeys"in e])>=4}function M(){var e=window;return L(["safari"in e,!("DeviceMotionEvent"in e),!("ongestureend"in e),!("standalone"in navigator)])>=3}function Y(){var e,n,t=window;return L(["buildID"in navigator,"MozAppearance"in(null!==(n=null===(e=document.documentElement)||void 0===e?void 0:e.style)&&void 0!==n?n:{}),"onmozfullscreenchange"in t,"mozInnerScreenX"in t,"CSSMozDocumentRule"in t,"CanvasCaptureMediaStream"in t])>=4}function G(){var e=document;return e.fullscreenElement||e.msFullscreenElement||e.mozFullScreenElement||e.webkitFullscreenElement||null}function R(){var e=x(),n=Y();if(!e&&!n)return!1;var t=window;return L(["onorientationchange"in t,"orientation"in t,e&&!("SharedWorker"in t),n&&/android/i.test(navigator.appVersion)])>=2}function X(e){var n=new Error(e);return n.name=e,n}function j(e,n,o){var a,c,u;return void 0===o&&(o=50),t(this,void 0,void 0,(function(){var t,l;return r(this,(function(r){switch(r.label){case 0:t=document,r.label=1;case 1:return t.body?[3,3]:[4,i(o)];case 2:return r.sent(),[3,1];case 3:l=t.createElement("iframe"),r.label=4;case 4:return r.trys.push([4,,10,11]),[4,new Promise((function(e,r){var o=!1,a=function(){o=!0,e()};l.onload=a,l.onerror=function(e){o=!0,r(e)};var i=l.style;i.setProperty("display","block","important"),i.position="absolute",i.top="0",i.left="0",i.visibility="hidden",n&&"srcdoc"in l?l.srcdoc=n:l.src="about:blank",t.body.appendChild(l);var c=function(){var e,n;o||("complete"===(null===(n=null===(e=l.contentWindow)||void 0===e?void 0:e.document)||void 0===n?void 0:n.readyState)?a():setTimeout(c,10))};c()}))];case 5:r.sent(),r.label=6;case 6:return(null===(c=null===(a=l.contentWindow)||void 0===a?void 0:a.document)||void 0===c?void 0:c.body)?[3,8]:[4,i(o)];case 7:return r.sent(),[3,6];case 8:return[4,e(l,l.contentWindow)];case 9:return[2,r.sent()];case 10:return null===(u=l.parentNode)||void 0===u||u.removeChild(l),[7];case 11:return[2]}}))}))}function A(e){for(var n=function(e){for(var n,t,r="Unexpected syntax '".concat(e,"'"),o=/^\s*([a-z-]*)(.*)$/i.exec(e),a=o[1]||void 0,i={},c=/([.:#][\w-]+|\[.+?\])/gi,u=function(e,n){i[e]=i[e]||[],i[e].push(n)};;){var l=c.exec(o[2]);if(!l)break;var s=l[0];switch(s[0]){case".":u("class",s.slice(1));break;case"#":u("id",s.slice(1));break;case"[":var d=/^\[([\w-]+)([~|^$*]?=("(.*?)"|([\w-]+)))?(\s+[is])?\]$/.exec(s);if(!d)throw new Error(r);u(d[1],null!==(t=null!==(n=d[4])&&void 0!==n?n:d[5])&&void 0!==t?t:"");break;default:throw new Error(r)}}return[a,i]}(e),t=n[0],r=n[1],o=document.createElement(null!=t?t:"div"),a=0,i=Object.keys(r);a<i.length;a++){var c=i[a],u=r[c].join(" ");"style"===c?I(o.style,u):o.setAttribute(c,u)}return o}function I(e,n){for(var t=0,r=n.split(";");t<r.length;t++){var o=r[t],a=/^\s*([\w-]+)\s*:\s*(.+?)(\s*!([\w-]+))?\s*$/.exec(o);if(a){var i=a[1],c=a[2],u=a[4];e.setProperty(i,c,u||"")}}}var H=["monospace","sans-serif","serif"],J=["sans-serif-thin","ARNO PRO","Agency FB","Arabic Typesetting","Arial Unicode MS","AvantGarde Bk BT","BankGothic Md BT","Batang","Bitstream Vera Sans Mono","Calibri","Century","Century Gothic","Clarendon","EUROSTILE","Franklin Gothic","Futura Bk BT","Futura Md BT","GOTHAM","Gill Sans","HELV","Haettenschweiler","Helvetica Neue","Humanst521 BT","Leelawadee","Letter Gothic","Levenim MT","Lucida Bright","Lucida Sans","Menlo","MS Mincho","MS Outlook","MS Reference Specialty","MS UI Gothic","MT Extra","MYRIAD PRO","Marlett","Meiryo UI","Microsoft Uighur","Minion Pro","Monotype Corsiva","PMingLiU","Pristina","SCRIPTINA","Segoe UI Light","Serifa","SimHei","Small Fonts","Staccato222 BT","TRAJAN PRO","Univers CE 55 Medium","Vrinda","ZWAdobeF"];function P(e){return e.toDataURL()}var N,D;function z(){var e=this;return function(){if(void 0===D){var e=function(){var n=E();T(n)?D=setTimeout(e,2500):(N=n,D=void 0)};e()}}(),function(){return t(e,void 0,void 0,(function(){var e;return r(this,(function(n){switch(n.label){case 0:return T(e=E())?N?[2,o([],N,!0)]:G()?[4,(t=document,(t.exitFullscreen||t.msExitFullscreen||t.mozCancelFullScreen||t.webkitExitFullscreen).call(t))]:[3,2]:[3,2];case 1:n.sent(),e=E(),n.label=2;case 2:return T(e)||(N=e),[2,e]}var t}))}))}}function E(){var e=screen;return[w(g(e.availTop),null),w(g(e.width)-g(e.availWidth)-w(g(e.availLeft),0),null),w(g(e.height)-g(e.availHeight)-w(g(e.availTop),0),null),w(g(e.availLeft),null)]}function T(e){for(var n=0;n<4;++n)if(e[n])return!1;return!0}function B(e){var n;return t(this,void 0,void 0,(function(){var t,o,a,c,u,l,s;return r(this,(function(r){switch(r.label){case 0:for(t=document,o=t.createElement("div"),a=new Array(e.length),c={},_(o),s=0;s<e.length;++s)"DIALOG"===(u=A(e[s])).tagName&&u.show(),_(l=t.createElement("div")),l.appendChild(u),o.appendChild(l),a[s]=u;r.label=1;case 1:return t.body?[3,3]:[4,i(50)];case 2:return r.sent(),[3,1];case 3:t.body.appendChild(o);try{for(s=0;s<e.length;++s)a[s].offsetParent||(c[e[s]]=!0)}finally{null===(n=o.parentNode)||void 0===n||n.removeChild(o)}return[2,c]}}))}))}function _(e){e.style.setProperty("display","block","important")}function O(e){return matchMedia("(inverted-colors: ".concat(e,")")).matches}function U(e){return matchMedia("(forced-colors: ".concat(e,")")).matches}function Q(e){return matchMedia("(prefers-contrast: ".concat(e,")")).matches}function K(e){return matchMedia("(prefers-reduced-motion: ".concat(e,")")).matches}function q(e){return matchMedia("(dynamic-range: ".concat(e,")")).matches}var $=Math,ee=function(){return 0};var ne={default:[],apple:[{font:"-apple-system-body"}],serif:[{fontFamily:"serif"}],sans:[{fontFamily:"sans-serif"}],mono:[{fontFamily:"monospace"}],min:[{fontSize:"1px"}],system:[{fontFamily:"system-ui"}]};var te={fonts:function(){return j((function(e,n){var t=n.document,r=t.body;r.style.fontSize="48px";var o=t.createElement("div"),a={},i={},c=function(e){var n=t.createElement("span"),r=n.style;return r.position="absolute",r.top="0",r.left="0",r.fontFamily=e,n.textContent="mmMwWLliI0O&1",o.appendChild(n),n},u=H.map(c),l=function(){for(var e={},n=function(n){e[n]=H.map((function(e){return function(e,n){return c("'".concat(e,"',").concat(n))}(n,e)}))},t=0,r=J;t<r.length;t++){n(r[t])}return e}();r.appendChild(o);for(var s=0;s<H.length;s++)a[H[s]]=u[s].offsetWidth,i[H[s]]=u[s].offsetHeight;return J.filter((function(e){return n=l[e],H.some((function(e,t){return n[t].offsetWidth!==a[e]||n[t].offsetHeight!==i[e]}));var n}))}))},domBlockers:function(e){var n=(void 0===e?{}:e).debug;return t(this,void 0,void 0,(function(){var e,t,o,a,i;return r(this,(function(r){switch(r.label){case 0:return F()||R()?(c=atob,e={abpIndo:["#Iklan-Melayang","#Kolom-Iklan-728","#SidebarIklan-wrapper",'[title="ALIENBOLA" i]',c("I0JveC1CYW5uZXItYWRz")],abpvn:[".quangcao","#mobileCatfish",c("LmNsb3NlLWFkcw=="),'[id^="bn_bottom_fixed_"]',"#pmadv"],adBlockFinland:[".mainostila",c("LnNwb25zb3JpdA=="),".ylamainos",c("YVtocmVmKj0iL2NsaWNrdGhyZ2guYXNwPyJd"),c("YVtocmVmXj0iaHR0cHM6Ly9hcHAucmVhZHBlYWsuY29tL2FkcyJd")],adBlockPersian:["#navbar_notice_50",".kadr",'TABLE[width="140px"]',"#divAgahi",c("YVtocmVmXj0iaHR0cDovL2cxLnYuZndtcm0ubmV0L2FkLyJd")],adBlockWarningRemoval:["#adblock-honeypot",".adblocker-root",".wp_adblock_detect",c("LmhlYWRlci1ibG9ja2VkLWFk"),c("I2FkX2Jsb2NrZXI=")],adGuardAnnoyances:[".hs-sosyal","#cookieconsentdiv",'div[class^="app_gdpr"]',".as-oil",'[data-cypress="soft-push-notification-modal"]'],adGuardBase:[".BetterJsPopOverlay",c("I2FkXzMwMFgyNTA="),c("I2Jhbm5lcmZsb2F0MjI="),c("I2NhbXBhaWduLWJhbm5lcg=="),c("I0FkLUNvbnRlbnQ=")],adGuardChinese:[c("LlppX2FkX2FfSA=="),c("YVtocmVmKj0iLmh0aGJldDM0LmNvbSJd"),"#widget-quan",c("YVtocmVmKj0iLzg0OTkyMDIwLnh5eiJd"),c("YVtocmVmKj0iLjE5NTZobC5jb20vIl0=")],adGuardFrench:["#pavePub",c("LmFkLWRlc2t0b3AtcmVjdGFuZ2xl"),".mobile_adhesion",".widgetadv",c("LmFkc19iYW4=")],adGuardGerman:['aside[data-portal-id="leaderboard"]'],adGuardJapanese:["#kauli_yad_1",c("YVtocmVmXj0iaHR0cDovL2FkMi50cmFmZmljZ2F0ZS5uZXQvIl0="),c("Ll9wb3BJbl9pbmZpbml0ZV9hZA=="),c("LmFkZ29vZ2xl"),c("Ll9faXNib29zdFJldHVybkFk")],adGuardMobile:[c("YW1wLWF1dG8tYWRz"),c("LmFtcF9hZA=="),'amp-embed[type="24smi"]',"#mgid_iframe1",c("I2FkX2ludmlld19hcmVh")],adGuardRussian:[c("YVtocmVmXj0iaHR0cHM6Ly9hZC5sZXRtZWFkcy5jb20vIl0="),c("LnJlY2xhbWE="),'div[id^="smi2adblock"]',c("ZGl2W2lkXj0iQWRGb3hfYmFubmVyXyJd"),"#psyduckpockeball"],adGuardSocial:[c("YVtocmVmXj0iLy93d3cuc3R1bWJsZXVwb24uY29tL3N1Ym1pdD91cmw9Il0="),c("YVtocmVmXj0iLy90ZWxlZ3JhbS5tZS9zaGFyZS91cmw/Il0="),".etsy-tweet","#inlineShare",".popup-social"],adGuardSpanishPortuguese:["#barraPublicidade","#Publicidade","#publiEspecial","#queTooltip",".cnt-publi"],adGuardTrackingProtection:["#qoo-counter",c("YVtocmVmXj0iaHR0cDovL2NsaWNrLmhvdGxvZy5ydS8iXQ=="),c("YVtocmVmXj0iaHR0cDovL2hpdGNvdW50ZXIucnUvdG9wL3N0YXQucGhwIl0="),c("YVtocmVmXj0iaHR0cDovL3RvcC5tYWlsLnJ1L2p1bXAiXQ=="),"#top100counter"],adGuardTurkish:["#backkapat",c("I3Jla2xhbWk="),c("YVtocmVmXj0iaHR0cDovL2Fkc2Vydi5vbnRlay5jb20udHIvIl0="),c("YVtocmVmXj0iaHR0cDovL2l6bGVuemkuY29tL2NhbXBhaWduLyJd"),c("YVtocmVmXj0iaHR0cDovL3d3dy5pbnN0YWxsYWRzLm5ldC8iXQ==")],bulgarian:[c("dGQjZnJlZW5ldF90YWJsZV9hZHM="),"#ea_intext_div",".lapni-pop-over","#xenium_hot_offers"],easyList:[".yb-floorad",c("LndpZGdldF9wb19hZHNfd2lkZ2V0"),c("LnRyYWZmaWNqdW5reS1hZA=="),".textad_headline",c("LnNwb25zb3JlZC10ZXh0LWxpbmtz")],easyListChina:[c("LmFwcGd1aWRlLXdyYXBbb25jbGljayo9ImJjZWJvcy5jb20iXQ=="),c("LmZyb250cGFnZUFkdk0="),"#taotaole","#aafoot.top_box",".cfa_popup"],easyListCookie:[".ezmob-footer",".cc-CookieWarning","[data-cookie-number]",c("LmF3LWNvb2tpZS1iYW5uZXI="),".sygnal24-gdpr-modal-wrap"],easyListCzechSlovak:["#onlajny-stickers",c("I3Jla2xhbW5pLWJveA=="),c("LnJla2xhbWEtbWVnYWJvYXJk"),".sklik",c("W2lkXj0ic2tsaWtSZWtsYW1hIl0=")],easyListDutch:[c("I2FkdmVydGVudGll"),c("I3ZpcEFkbWFya3RCYW5uZXJCbG9jaw=="),".adstekst",c("YVtocmVmXj0iaHR0cHM6Ly94bHR1YmUubmwvY2xpY2svIl0="),"#semilo-lrectangle"],easyListGermany:["#SSpotIMPopSlider",c("LnNwb25zb3JsaW5rZ3J1ZW4="),c("I3dlcmJ1bmdza3k="),c("I3Jla2xhbWUtcmVjaHRzLW1pdHRl"),c("YVtocmVmXj0iaHR0cHM6Ly9iZDc0Mi5jb20vIl0=")],easyListItaly:[c("LmJveF9hZHZfYW5udW5jaQ=="),".sb-box-pubbliredazionale",c("YVtocmVmXj0iaHR0cDovL2FmZmlsaWF6aW9uaWFkcy5zbmFpLml0LyJd"),c("YVtocmVmXj0iaHR0cHM6Ly9hZHNlcnZlci5odG1sLml0LyJd"),c("YVtocmVmXj0iaHR0cHM6Ly9hZmZpbGlhemlvbmlhZHMuc25haS5pdC8iXQ==")],easyListLithuania:[c("LnJla2xhbW9zX3RhcnBhcw=="),c("LnJla2xhbW9zX251b3JvZG9z"),c("aW1nW2FsdD0iUmVrbGFtaW5pcyBza3lkZWxpcyJd"),c("aW1nW2FsdD0iRGVkaWt1b3RpLmx0IHNlcnZlcmlhaSJd"),c("aW1nW2FsdD0iSG9zdGluZ2FzIFNlcnZlcmlhaS5sdCJd")],estonian:[c("QVtocmVmKj0iaHR0cDovL3BheTRyZXN1bHRzMjQuZXUiXQ==")],fanboyAnnoyances:["#ac-lre-player",".navigate-to-top","#subscribe_popup",".newsletter_holder","#back-top"],fanboyAntiFacebook:[".util-bar-module-firefly-visible"],fanboyEnhancedTrackers:[".open.pushModal","#issuem-leaky-paywall-articles-zero-remaining-nag","#sovrn_container",'div[class$="-hide"][zoompage-fontsize][style="display: block;"]',".BlockNag__Card"],fanboySocial:["#FollowUs","#meteored_share","#social_follow",".article-sharer",".community__social-desc"],frellwitSwedish:[c("YVtocmVmKj0iY2FzaW5vcHJvLnNlIl1bdGFyZ2V0PSJfYmxhbmsiXQ=="),c("YVtocmVmKj0iZG9rdG9yLXNlLm9uZWxpbmsubWUiXQ=="),"article.category-samarbete",c("ZGl2LmhvbGlkQWRz"),"ul.adsmodern"],greekAdBlock:[c("QVtocmVmKj0iYWRtYW4ub3RlbmV0LmdyL2NsaWNrPyJd"),c("QVtocmVmKj0iaHR0cDovL2F4aWFiYW5uZXJzLmV4b2R1cy5nci8iXQ=="),c("QVtocmVmKj0iaHR0cDovL2ludGVyYWN0aXZlLmZvcnRobmV0LmdyL2NsaWNrPyJd"),"DIV.agores300","TABLE.advright"],hungarian:["#cemp_doboz",".optimonk-iframe-container",c("LmFkX19tYWlu"),c("W2NsYXNzKj0iR29vZ2xlQWRzIl0="),"#hirdetesek_box"],iDontCareAboutCookies:['.alert-info[data-block-track*="CookieNotice"]',".ModuleTemplateCookieIndicator",".o--cookies--container","#cookies-policy-sticky","#stickyCookieBar"],icelandicAbp:[c("QVtocmVmXj0iL2ZyYW1ld29yay9yZXNvdXJjZXMvZm9ybXMvYWRzLmFzcHgiXQ==")],latvian:[c("YVtocmVmPSJodHRwOi8vd3d3LnNhbGlkemluaS5sdi8iXVtzdHlsZT0iZGlzcGxheTogYmxvY2s7IHdpZHRoOiAxMjBweDsgaGVpZ2h0OiA0MHB4OyBvdmVyZmxvdzogaGlkZGVuOyBwb3NpdGlvbjogcmVsYXRpdmU7Il0="),c("YVtocmVmPSJodHRwOi8vd3d3LnNhbGlkemluaS5sdi8iXVtzdHlsZT0iZGlzcGxheTogYmxvY2s7IHdpZHRoOiA4OHB4OyBoZWlnaHQ6IDMxcHg7IG92ZXJmbG93OiBoaWRkZW47IHBvc2l0aW9uOiByZWxhdGl2ZTsiXQ==")],listKr:[c("YVtocmVmKj0iLy9hZC5wbGFuYnBsdXMuY28ua3IvIl0="),c("I2xpdmVyZUFkV3JhcHBlcg=="),c("YVtocmVmKj0iLy9hZHYuaW1hZHJlcC5jby5rci8iXQ=="),c("aW5zLmZhc3R2aWV3LWFk"),".revenue_unit_item.dable"],listeAr:[c("LmdlbWluaUxCMUFk"),".right-and-left-sponsers",c("YVtocmVmKj0iLmFmbGFtLmluZm8iXQ=="),c("YVtocmVmKj0iYm9vcmFxLm9yZyJd"),c("YVtocmVmKj0iZHViaXp6bGUuY29tL2FyLz91dG1fc291cmNlPSJd")],listeFr:[c("YVtocmVmXj0iaHR0cDovL3Byb21vLnZhZG9yLmNvbS8iXQ=="),c("I2FkY29udGFpbmVyX3JlY2hlcmNoZQ=="),c("YVtocmVmKj0id2Vib3JhbWEuZnIvZmNnaS1iaW4vIl0="),".site-pub-interstitiel",'div[id^="crt-"][data-criteo-id]'],officialPolish:["#ceneo-placeholder-ceneo-12",c("W2hyZWZePSJodHRwczovL2FmZi5zZW5kaHViLnBsLyJd"),c("YVtocmVmXj0iaHR0cDovL2Fkdm1hbmFnZXIudGVjaGZ1bi5wbC9yZWRpcmVjdC8iXQ=="),c("YVtocmVmXj0iaHR0cDovL3d3dy50cml6ZXIucGwvP3V0bV9zb3VyY2UiXQ=="),c("ZGl2I3NrYXBpZWNfYWQ=")],ro:[c("YVtocmVmXj0iLy9hZmZ0cmsuYWx0ZXgucm8vQ291bnRlci9DbGljayJd"),c("YVtocmVmXj0iaHR0cHM6Ly9ibGFja2ZyaWRheXNhbGVzLnJvL3Ryay9zaG9wLyJd"),c("YVtocmVmXj0iaHR0cHM6Ly9ldmVudC4ycGVyZm9ybWFudC5jb20vZXZlbnRzL2NsaWNrIl0="),c("YVtocmVmXj0iaHR0cHM6Ly9sLnByb2ZpdHNoYXJlLnJvLyJd"),'a[href^="/url/"]'],ruAd:[c("YVtocmVmKj0iLy9mZWJyYXJlLnJ1LyJd"),c("YVtocmVmKj0iLy91dGltZy5ydS8iXQ=="),c("YVtocmVmKj0iOi8vY2hpa2lkaWtpLnJ1Il0="),"#pgeldiz",".yandex-rtb-block"],thaiAds:["a[href*=macau-uta-popup]",c("I2Fkcy1nb29nbGUtbWlkZGxlX3JlY3RhbmdsZS1ncm91cA=="),c("LmFkczMwMHM="),".bumq",".img-kosana"],webAnnoyancesUltralist:["#mod-social-share-2","#social-tools",c("LmN0cGwtZnVsbGJhbm5lcg=="),".zergnet-recommend",".yt.btn-link.btn-md.btn"]},t=Object.keys(e),[4,B((i=[]).concat.apply(i,t.map((function(n){return e[n]}))))]):[2,void 0];case 1:return o=r.sent(),n&&function(e,n){for(var t="DOM blockers debug:\n```",r=0,o=Object.keys(e);r<o.length;r++){var a=o[r];t+="\n".concat(a,":");for(var i=0,c=e[a];i<c.length;i++){var u=c[i];t+="\n  ".concat(n[u]?"🚫":"➡️"," ").concat(u)}}console.log("".concat(t,"\n```"))}(e,o),(a=t.filter((function(n){var t=e[n];return L(t.map((function(e){return o[e]})))>.6*t.length}))).sort(),[2,a]}var c}))}))},fontPreferences:function(){return function(e,n){void 0===n&&(n=4e3);return j((function(t,r){var a=r.document,i=a.body,c=i.style;c.width="".concat(n,"px"),c.webkitTextSizeAdjust=c.textSizeAdjust="none",x()?i.style.zoom="".concat(1/r.devicePixelRatio):F()&&(i.style.zoom="reset");var u=a.createElement("div");return u.textContent=o([],Array(n/20<<0),!0).map((function(){return"word"})).join(" "),i.appendChild(u),e(a,i)}),'<!doctype html><html><head><meta name="viewport" content="width=device-width, initial-scale=1">')}((function(e,n){for(var t={},r={},o=0,a=Object.keys(ne);o<a.length;o++){var i=a[o],c=ne[i],u=c[0],l=void 0===u?{}:u,s=c[1],d=void 0===s?"mmMwWLliI0fiflO&1":s,m=e.createElement("span");m.textContent=d,m.style.whiteSpace="nowrap";for(var f=0,v=Object.keys(l);f<v.length;f++){var h=v[f],p=l[h];void 0!==p&&(m.style[h]=p)}t[i]=m,n.appendChild(e.createElement("br")),n.appendChild(m)}for(var b=0,y=Object.keys(ne);b<y.length;b++){r[i=y[b]]=t[i].getBoundingClientRect().width}return r}))},audio:function(){var e=window,n=e.OfflineAudioContext||e.webkitOfflineAudioContext;if(!n)return-2;if(F()&&!M()&&!function(){var e=window;return L(["DOMRectList"in e,"RTCPeerConnectionIceEvent"in e,"SVGGeometryElement"in e,"ontransitioncancel"in e])>=3}())return-1;var t=new n(1,5e3,44100),r=t.createOscillator();r.type="triangle",r.frequency.value=1e4;var o=t.createDynamicsCompressor();o.threshold.value=-50,o.knee.value=40,o.ratio.value=12,o.attack.value=0,o.release.value=.25,r.connect(o),o.connect(t.destination),r.start(0);var a=function(e){var n=3,t=500,r=500,o=5e3,a=function(){},i=new Promise((function(i,u){var l=!1,d=0,m=0;e.oncomplete=function(e){return i(e.renderedBuffer)};var f=function(){setTimeout((function(){return u(X("timeout"))}),Math.min(r,m+o-Date.now()))},v=function(){try{var r=e.startRendering();switch(c(r)&&s(r),e.state){case"running":m=Date.now(),l&&f();break;case"suspended":document.hidden||d++,l&&d>=n?u(X("suspended")):setTimeout(v,t)}}catch(o){u(o)}};v(),a=function(){l||(l=!0,m>0&&f())}}));return[i,a]}(t),i=a[0],u=a[1],l=i.then((function(e){return function(e){for(var n=0,t=0;t<e.length;++t)n+=Math.abs(e[t]);return n}(e.getChannelData(0).subarray(4500))}),(function(e){if("timeout"===e.name||"suspended"===e.name)return-3;throw e}));return s(l),function(){return u(),l}},screenFrame:function(){var e=this,n=z();return function(){return t(e,void 0,void 0,(function(){var e,t;return r(this,(function(r){switch(r.label){case 0:return[4,n()];case 1:return e=r.sent(),[2,[(t=function(e){return null===e?null:k(e,10)})(e[0]),t(e[1]),t(e[2]),t(e[3])]]}}))}))}},osCpu:function(){return navigator.oscpu},languages:function(){var e,n=navigator,t=[],r=n.language||n.userLanguage||n.browserLanguage||n.systemLanguage;if(void 0!==r&&t.push([r]),Array.isArray(n.languages))x()&&L([!("MediaSettingsRange"in(e=window)),"RTCEncodedAudioFrame"in e,""+e.Intl=="[object Intl]",""+e.Reflect=="[object Reflect]"])>=3||t.push(n.languages);else if("string"==typeof n.languages){var o=n.languages;o&&t.push(o.split(","))}return t},colorDepth:function(){return window.screen.colorDepth},deviceMemory:function(){return w(g(navigator.deviceMemory),void 0)},screenResolution:function(){var e=screen,n=function(e){return w(y(e),null)},t=[n(e.width),n(e.height)];return t.sort().reverse(),t},hardwareConcurrency:function(){return w(y(navigator.hardwareConcurrency),void 0)},timezone:function(){var e,n=null===(e=window.Intl)||void 0===e?void 0:e.DateTimeFormat;if(n){var t=(new n).resolvedOptions().timeZone;if(t)return t}var r,o=(r=(new Date).getFullYear(),-Math.max(g(new Date(r,0,1).getTimezoneOffset()),g(new Date(r,6,1).getTimezoneOffset())));return"UTC".concat(o>=0?"+":"").concat(Math.abs(o))},sessionStorage:function(){try{return!!window.sessionStorage}catch(e){return!0}},localStorage:function(){try{return!!window.localStorage}catch(e){return!0}},indexedDB:function(){if(!S()&&!C())try{return!!window.indexedDB}catch(e){return!0}},openDatabase:function(){return!!window.openDatabase},cpuClass:function(){return navigator.cpuClass},platform:function(){var e=navigator.platform;return"MacIntel"===e&&F()&&!M()?function(){if("iPad"===navigator.platform)return!0;var e=screen,n=e.width/e.height;return L(["MediaSource"in window,!!Element.prototype.webkitRequestFullscreen,n>.65&&n<1.53])>=2}()?"iPad":"iPhone":e},plugins:function(){var e=navigator.plugins;if(e){for(var n=[],t=0;t<e.length;++t){var r=e[t];if(r){for(var o=[],a=0;a<r.length;++a){var i=r[a];o.push({type:i.type,suffixes:i.suffixes})}n.push({name:r.name,description:r.description,mimeTypes:o})}}return n}},canvas:function(){var e,n,t=!1,r=function(){var e=document.createElement("canvas");return e.width=1,e.height=1,[e,e.getContext("2d")]}(),o=r[0],a=r[1];if(function(e,n){return!(!n||!e.toDataURL)}(o,a)){t=function(e){return e.rect(0,0,10,10),e.rect(2,2,6,6),!e.isPointInPath(5,5,"evenodd")}(a),function(e,n){e.width=240,e.height=60,n.textBaseline="alphabetic",n.fillStyle="#f60",n.fillRect(100,1,62,20),n.fillStyle="#069",n.font='11pt "Times New Roman"';var t="Cwm fjordbank gly ".concat(String.fromCharCode(55357,56835));n.fillText(t,2,15),n.fillStyle="rgba(102, 204, 0, 0.2)",n.font="18pt Arial",n.fillText(t,4,45)}(o,a);var i=P(o);i!==P(o)?e=n="unstable":(n=i,function(e,n){e.width=122,e.height=110,n.globalCompositeOperation="multiply";for(var t=0,r=[["#f2f",40,40],["#2ff",80,40],["#ff2",60,80]];t<r.length;t++){var o=r[t],a=o[0],i=o[1],c=o[2];n.fillStyle=a,n.beginPath(),n.arc(i,c,40,0,2*Math.PI,!0),n.closePath(),n.fill()}n.fillStyle="#f9c",n.arc(60,60,60,0,2*Math.PI,!0),n.arc(60,60,20,0,2*Math.PI,!0),n.fill("evenodd")}(o,a),e=P(o))}else e=n="";return{winding:t,geometry:e,text:n}},touchSupport:function(){var e,n=navigator,t=0;void 0!==n.maxTouchPoints?t=y(n.maxTouchPoints):void 0!==n.msMaxTouchPoints&&(t=n.msMaxTouchPoints);try{document.createEvent("TouchEvent"),e=!0}catch(r){e=!1}return{maxTouchPoints:t,touchEvent:e,touchStart:"ontouchstart"in window}},vendor:function(){return navigator.vendor||""},vendorFlavors:function(){for(var e=[],n=0,t=["chrome","safari","__crWeb","__gCrWeb","yandex","__yb","__ybro","__firefox__","__edgeTrackingPreventionStatistics","webkit","oprt","samsungAr","ucweb","UCShellJava","puffinDevice"];n<t.length;n++){var r=t[n],o=window[r];o&&"object"==typeof o&&e.push(r)}return e.sort()},cookiesEnabled:function(){var e=document;try{e.cookie="cookietest=1; SameSite=Strict;";var n=-1!==e.cookie.indexOf("cookietest=");return e.cookie="cookietest=1; SameSite=Strict; expires=Thu, 01-Jan-1970 00:00:01 GMT",n}catch(t){return!1}},colorGamut:function(){for(var e=0,n=["rec2020","p3","srgb"];e<n.length;e++){var t=n[e];if(matchMedia("(color-gamut: ".concat(t,")")).matches)return t}},invertedColors:function(){return!!O("inverted")||!O("none")&&void 0},forcedColors:function(){return!!U("active")||!U("none")&&void 0},monochrome:function(){if(matchMedia("(min-monochrome: 0)").matches){for(var e=0;e<=100;++e)if(matchMedia("(max-monochrome: ".concat(e,")")).matches)return e;throw new Error("Too high value")}},contrast:function(){return Q("no-preference")?0:Q("high")||Q("more")?1:Q("low")||Q("less")?-1:Q("forced")?10:void 0},reducedMotion:function(){return!!K("reduce")||!K("no-preference")&&void 0},hdr:function(){return!!q("high")||!q("standard")&&void 0},math:function(){var e,n=$.acos||ee,t=$.acosh||ee,r=$.asin||ee,o=$.asinh||ee,a=$.atanh||ee,i=$.atan||ee,c=$.sin||ee,u=$.sinh||ee,l=$.cos||ee,s=$.cosh||ee,d=$.tan||ee,m=$.tanh||ee,f=$.exp||ee,v=$.expm1||ee,h=$.log1p||ee;return{acos:n(.12312423423423424),acosh:t(1e308),acoshPf:(e=1e154,$.log(e+$.sqrt(e*e-1))),asin:r(.12312423423423424),asinh:o(1),asinhPf:function(e){return $.log(e+$.sqrt(e*e+1))}(1),atanh:a(.5),atanhPf:function(e){return $.log((1+e)/(1-e))/2}(.5),atan:i(.5),sin:c(-1e300),sinh:u(1),sinhPf:function(e){return $.exp(e)-1/$.exp(e)/2}(1),cos:l(10.000000000123),cosh:s(1),coshPf:function(e){return($.exp(e)+1/$.exp(e))/2}(1),tan:d(-1e300),tanh:m(1),tanhPf:function(e){return($.exp(2*e)-1)/($.exp(2*e)+1)}(1),exp:f(1),expm1:v(1),expm1Pf:function(e){return $.exp(e)-1}(1),log1p:h(10),log1pPf:function(e){return $.log(1+e)}(10),powPI:function(e){return $.pow($.PI,e)}(-100)}},videoCard:function(){var e,n=document.createElement("canvas"),t=null!==(e=n.getContext("webgl"))&&void 0!==e?e:n.getContext("experimental-webgl");if(t&&"getExtension"in t){var r=t.getExtension("WEBGL_debug_renderer_info");if(r)return{vendor:(t.getParameter(r.UNMASKED_VENDOR_WEBGL)||"").toString(),renderer:(t.getParameter(r.UNMASKED_RENDERER_WEBGL)||"").toString()}}},pdfViewerEnabled:function(){return navigator.pdfViewerEnabled},architecture:function(){var e=new Float32Array(1),n=new Uint8Array(e.buffer);return e[0]=1/0,e[0]=e[0]-e[0],n[3]}};function re(e){var n=function(e){if(R())return.4;if(F())return M()?.5:.3;var n=e.platform.value||"";if(/^Win/.test(n))return.6;if(/^Mac/.test(n))return.5;return.7}(e),t=function(e){return k(.99+.01*e,1e-4)}(n);return{score:n,comment:"$ if upgrade to Pro: https://fpjs.dev/pro".replace(/\$/g,"".concat(t))}}function oe(e){return JSON.stringify(e,(function(e,t){return t instanceof Error?n({name:(r=t).name,message:r.message,stack:null===(o=r.stack)||void 0===o?void 0:o.split("\n")},r):t;var r,o}),2)}function ae(e){return b(function(e){for(var n="",t=0,r=Object.keys(e).sort();t<r.length;t++){var o=r[t],a=e[o],i=a.error?"error":JSON.stringify(a.value);n+="".concat(n?"|":"").concat(o.replace(/([:|\\])/g,"\\$1"),":").concat(i)}return n}(e))}function ie(e){return void 0===e&&(e=50),function(e,n){void 0===n&&(n=1/0);var t=window.requestIdleCallback;return t?new Promise((function(e){return t.call(window,(function(){return e()}),{timeout:n})})):i(Math.min(e,n))}(e,2*e)}function ce(e,n){var o=Date.now();return{get:function(i){return t(this,void 0,void 0,(function(){var t,c,u;return r(this,(function(r){switch(r.label){case 0:return t=Date.now(),[4,e()];case 1:return c=r.sent(),u=function(e){var n;return{get visitorId(){return void 0===n&&(n=ae(this.components)),n},set visitorId(e){n=e},confidence:re(e),components:e,version:a}}(c),(n||(null==i?void 0:i.debug))&&console.log("Copy the text below to get the debug data:\n\n```\nversion: ".concat(u.version,"\nuserAgent: ").concat(navigator.userAgent,"\ntimeBetweenLoadAndGet: ").concat(t-o,"\nvisitorId: ").concat(u.visitorId,"\ncomponents: ").concat(oe(c),"\n```")),[2,u]}}))}))}}}function ue(e){var n=void 0===e?{}:e,o=n.delayFallback,i=n.debug,c=n.monitoring,u=void 0===c||c;return t(this,void 0,void 0,(function(){return r(this,(function(e){switch(e.label){case 0:return u&&function(){if(!(window.__fpjs_d_m||Math.random()>=.001))try{var e=new XMLHttpRequest;e.open("get","https://m1.openfpcdn.io/fingerprintjs/v".concat(a,"/npm-monitoring"),!0),e.send()}catch(n){console.error(n)}}(),[4,ie(o)];case 1:return e.sent(),[2,ce(Z(te,{debug:i},[]),i)]}}))}))}var le={load:ue,hashComponents:ae,componentsToDebugString:oe},se=b;return e.componentsToDebugString=oe,e.default=le,e.getFullscreenElement=G,e.getScreenFrame=z,e.hashComponents=ae,e.isAndroid=R,e.isChromium=x,e.isDesktopSafari=M,e.isEdgeHTML=C,e.isGecko=Y,e.isTrident=S,e.isWebKit=F,e.load=ue,e.loadSources=Z,e.murmurX64Hash128=se,e.prepareForSources=ie,e.sources=te,e.transformSource=function(e,n){var t=function(e){return W(e)?n(e):function(){var t=e();return c(t)?t.then(n):n(t)}};return function(n){var r=e(n);return c(r)?r.then(t):t(r)}},e.withIframe=j,Object.defineProperty(e,"__esModule",{value:!0}),e}({});

function updateMembershipDescription() {
    const selectedValue = $('#MembershipCategory').val();
    const selected = membershipCategories.find(c => c.name === selectedValue);

    $('#finance-section').toggle(!!selected?.financeAvailable);

    let descriptionText = selected?.description || '';

    const hasReferrer = $('#ReferrerId').val();
    if (hasReferrer) {
        descriptionText += selected?.referrerEligable
            ? ' <strong>Great news – this category qualifies for our referral reward scheme.</strong>'
            : ' <strong>Note: This category is not eligible for the referral reward scheme.</strong>';
    }

    if (selected) {
        const isWaitingList = !!selected.isOnWaitingList;

        const waitingListHtml = isWaitingList
            ? `<div class="waiting-list-message">This category currently has a waiting list.</div>`
            : ``;

        const html = `
<div class="membership-info${isWaitingList ? ' waiting-list' : ''}">
  <p class="membership-price">Price: ${selected.price}</p>
  <p class="membership-description">${descriptionText}</p>
  ${waitingListHtml}
</div>`;

        $('#membership-description')
            .html(html)
            .show();
    } else {
        $('#membership-description')
            .empty()
            .hide();
    }
}

window.enableAutocomplete = async function (apiKey) {
    await getAddress.autocomplete('AddressLine1', apiKey, {
        selected: (address) => {
            const line1 = address.formatted_address[0] || ''
            const line2 = address.formatted_address[1] || ''
            const townRaw = address.formatted_address[3] || ''
            const county = address.formatted_address[4] || ''
            const postcode = address.postcode || ''

            // Clean and assign line1
            document.getElementById('AddressLine1').value = line1

            // Conditional adjustment for line2 and town
            if (townRaw.includes(',') && !line2) {
                const [before, after] = townRaw.split(',').map(s => s.trim())
                document.getElementById('AddressLine2').value = before
                document.getElementById('Town').value = after
            } else {
                document.getElementById('AddressLine2').value = line2
                document.getElementById('Town').value = townRaw
            }

            document.getElementById('County').value = county
            document.getElementById('Postcode').value = postcode
        }
    })
}

$(function () {
    updateMembershipDescription();

    $('#MembershipCategory').on('change', updateMembershipDescription);
    $('#HasCdhId').on('change', function () {
        $('#cdh-section').toggle(this.checked);
    });

    $('form').on('submit', function (e) {
        if (!$(this).valid()) {
            return false;
        }
        $('#submit-button').attr('disabled', true).attr('aria-busy', 'true');
        $('#form-loading').removeClass('sr-only').attr('aria-hidden', 'false');
        return true;
    });
    
    $('#genderNote').toggle($('#Gender').val() === 'Other');
    $('#cdh-section').toggle($('#HasCdhId').is(':checked'));

    // Mutual messaging for form size
    let lastHeight = 0;

    function debounce(func, wait = 100) {
        let timeout;
        return function (...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), wait);
        };
    }
    function sendHeight() {
        const formContainer = document.querySelector('main') || document.body;
        const newHeight = formContainer.scrollHeight;
        if (Math.abs(newHeight - lastHeight) > 5) {  
            console.log('📏 Sending height:', newHeight);
            window.parent.postMessage({ frameHeight: newHeight }, window.location.origin);
            lastHeight = newHeight;
        }
    }

    // 👇 Wrap the sendHeight
    const debouncedSendHeight = debounce(sendHeight, 100);

    // 👇 MutationObserver using debouncedSendHeight
    const observer = new MutationObserver(debouncedSendHeight);
    observer.observe(document.body, { attributes: true, childList: true, subtree: true });

    // 👇 Resize listener using debouncedSendHeight
    window.addEventListener('resize', debouncedSendHeight);

    // 👇 Trigger once on load to set initial size
    sendHeight();
});

// FingerprintJS init
FingerprintJS.load().then(fp => fp.get()).then(result => {
    const fingerprint = result.visitorId;
    const fingerprintInput = document.getElementById('Fingerprint');
    if (fingerprintInput) {
        fingerprintInput.value = fingerprint;
    }
});


