var request = require('request');
var ids, names;

function getBuses(i)
{
  // console.log('trying to get buses #' + names[i]);

  request('http://bus.admomsk.ru/index.php/getroute/getbus/' + ids[i], function (error, response, body)
  {
    var json = JSON.parse(body);

    if (json.vehicles.length)
    {
      console.log(names[i] + ': ' + json.vehicles.length + ' на маршруте');
    }

    getBuses(++i);
  });
}

request('http://t.bus55.ru/index.php/app/get_routes/', function (error, response, body)
{
  ids = body.match(/get_stations\/([0-9]+)/g);
  names = body.match(/[0-9а-я]+<span class="caption"> [а-я ]+/g);

  for (var i = 0; i < ids.length; i++)
  {
    ids[i] = ids[i].slice(13);
  }

  for (var i = 0; i < names.length; i++)
  {
    names[i] = names[i].replace('<span class="caption">', '');
  }

  getBuses(0);
});

//http://t.bus55.ru/index.php/app/get_routes/
//http://bus.admomsk.ru/index.php/getroute/getbus/1