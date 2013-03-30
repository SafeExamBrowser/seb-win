$.fn.dataTableExt.oApi.fnFindCellRowNodes = function ( oSettings, sSearch, iColumn )
{
    var
        i,iLen, j, jLen,
        aOut = [], aData;
      
    for ( i=0, iLen=oSettings.aoData.length ; i<iLen ; i++ )
    {
        aData = oSettings.aoData[i]._aData;
          
        if ( typeof iColumn == 'undefined' )
        {
            for ( j=0, jLen=aData.length ; j<jLen ; j++ )
            {
                if ( aData[j] == sSearch )
                {
                    aOut.push( oSettings.aoData[i].nTr );
                }
            }
        }
        else if ( aData[iColumn] == sSearch )
        {
            aOut.push( oSettings.aoData[i].nTr );
        }
    }
      
    return aOut;
};
